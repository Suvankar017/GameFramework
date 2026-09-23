using System;
using System.Collections.Generic;
using GameFramework.Monetization.Entitlements;
using GameFramework.Rewards;
using GameFramework.Runtime.Diagnostics;
using GameFramework.Runtime.Events;
using GameFramework.Runtime.Persistence;
using GameFramework.Runtime.Services;

namespace GameFramework.Monetization.Purchases
{
    /// <summary>
    /// Default <see cref="IPurchaseService"/>, orchestrating one <see cref="IPurchaseProvider"/> -
    /// see CLAUDE.md's Phase 15 brief, sections 3/14/17/23/24/41.
    ///
    /// <see cref="IRewardService"/>/<see cref="IEntitlementService"/> are resolved softly
    /// (<see cref="IServiceRegistry.TryGet{TService}"/>): a product referencing a reward/entitlement
    /// with the corresponding service unregistered logs a warning and simply does not grant that
    /// half of it, rather than throwing - the same graceful-degradation pattern
    /// <c>Presentation.PresentationService</c> already established for its own soft dependencies.
    /// </summary>
    public sealed class PurchaseService : IPurchaseService
    {
        private const string LogCategory = "Monetization.Purchases";
        private const string SaveKey = "GameFramework.Monetization.Purchases";
        private const int SaveVersion = 1;

        private readonly ProductCatalog _catalog;
        private readonly IPurchaseProvider _provider;
        private readonly IPurchaseValidator _validator;
        private readonly Dictionary<string, ProductDefinition> _definitions = new Dictionary<string, ProductDefinition>(StringComparer.Ordinal);
        private readonly HashSet<string> _processedTransactionIds = new HashSet<string>(StringComparer.Ordinal);
        private readonly Dictionary<string, Action<PurchaseResult>> _pendingCallbacks = new Dictionary<string, Action<PurchaseResult>>(StringComparer.Ordinal);

        private IPersistenceService _persistence;
        private IEventService _events;
        private ILoggingService _log;
        private IRewardService _rewards;
        private IEntitlementService _entitlements;
        private bool _isDirty;
        private string _lastError;

        public MonetizationProviderState State { get; private set; } = MonetizationProviderState.NotInitialized;

        public event Action<PurchaseResult> PurchaseCompleted;
        public event Action<PurchaseResult> PurchaseFailed;
        public event Action<RestoreResult> RestoreCompleted;

        public PurchaseService(ProductCatalog catalog, IPurchaseProvider provider, IPurchaseValidator validator = null)
        {
            _catalog = catalog;
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _validator = validator ?? new LocalPurchaseValidator();

            if (_catalog != null)
            {
                foreach (ProductDefinition definition in _catalog.Products)
                {
                    if (definition.Id.IsValid)
                    {
                        _definitions[definition.Id.Value] = definition;
                    }
                }
            }
        }

        public void Initialize(IServiceRegistry registry)
        {
            _persistence = registry.Get<IPersistenceService>();
            _events = registry.Get<IEventService>();
            registry.TryGet(out _log);
            registry.TryGet(out _rewards);
            registry.TryGet(out _entitlements);

            _provider.PurchaseUpdated += OnProviderPurchaseUpdated;

            Load();

            var definitionList = new List<ProductDefinition>(_definitions.Values);
            State = MonetizationProviderState.Initializing;
            try
            {
                _provider.Initialize(definitionList, success =>
                {
                    State = success ? MonetizationProviderState.Initialized : MonetizationProviderState.Failed;
                    if (!success)
                    {
                        _lastError = "Provider initialization failed.";
                        _log?.Log(LogLevel.Warning, LogCategory, "Purchase provider failed to initialize.");
                    }
                });
            }
            catch (Exception exception)
            {
                // Provider failure isolation: a broken store SDK leaves purchasing unavailable, it does
                // not fail this service's (or the framework's) startup.
                HandleProviderException(exception, "Initialize");
                State = MonetizationProviderState.Failed;
            }
        }

        public void Shutdown()
        {
            _provider.PurchaseUpdated -= OnProviderPurchaseUpdated;

            if (_isDirty)
            {
                Save();
            }
        }

        public IReadOnlyList<Product> GetProducts()
        {
            if (State == MonetizationProviderState.Initialized)
            {
                return _provider.GetProducts();
            }

            var fallback = new List<Product>(_definitions.Count);
            foreach (ProductDefinition definition in _definitions.Values)
            {
                fallback.Add(Product.Unavailable(definition.Id, definition.Type, definition.FallbackDisplayName));
            }

            return fallback;
        }

        public bool TryGetProduct(ProductId id, out Product product)
        {
            if (State == MonetizationProviderState.Initialized && _provider.TryGetProduct(id, out product))
            {
                return true;
            }

            if (_definitions.TryGetValue(id.Value ?? string.Empty, out ProductDefinition definition))
            {
                product = Product.Unavailable(id, definition.Type, definition.FallbackDisplayName);
                return true;
            }

            product = default;
            return false;
        }

        public void Purchase(ProductId id, Action<PurchaseResult> onComplete)
        {
            if (!_definitions.TryGetValue(id.Value ?? string.Empty, out ProductDefinition definition))
            {
                onComplete?.Invoke(PurchaseResult.Immediate(PurchaseResultKind.ProductUnavailable, id));
                return;
            }

            if (State != MonetizationProviderState.Initialized)
            {
                onComplete?.Invoke(PurchaseResult.Immediate(PurchaseResultKind.NotInitialized, id));
                return;
            }

            if (definition.Type != ProductType.Consumable && definition.GrantedEntitlementId.IsValid &&
                (_entitlements?.HasEntitlement(definition.GrantedEntitlementId) ?? false))
            {
                onComplete?.Invoke(PurchaseResult.Immediate(PurchaseResultKind.AlreadyOwned, id));
                return;
            }

            if (onComplete != null)
            {
                _pendingCallbacks[id.Value] = onComplete;
            }

            try
            {
                _provider.Purchase(id, result => HandleProviderResult(definition, result));
            }
            catch (Exception exception)
            {
                HandleProviderException(exception, "Purchase");
                _pendingCallbacks.Remove(id.Value);
                var failed = new PurchaseResult(PurchaseResultKind.Failed, id, string.Empty, "The purchase provider threw an exception.");
                RaiseFailed(failed);
                onComplete?.Invoke(failed);
            }
        }

        public void RestorePurchases(Action<RestoreResult> onComplete)
        {
            if (State != MonetizationProviderState.Initialized)
            {
                var failure = new RestoreResult(false, Array.Empty<ProductId>(), "Purchase provider is not initialized.");
                onComplete?.Invoke(failure);
                return;
            }

            Action<RestoreResult> handleRestore = result =>
            {
                if (result.Success && result.RestoredProductIds != null)
                {
                    foreach (ProductId productId in result.RestoredProductIds)
                    {
                        if (!_definitions.TryGetValue(productId.Value ?? string.Empty, out ProductDefinition definition))
                        {
                            // Never grant an unknown product - a provider may report ids this build's
                            // catalog doesn't define (removed/renamed products).
                            _log?.Log(LogLevel.Warning, LogCategory, $"Restore reported unknown product '{productId}'; ignored.");
                            continue;
                        }

                        string transactionId = "restore:" + productId.Value;
                        ProcessGrant(definition, PurchaseResult.Immediate(PurchaseResultKind.Restored, productId), transactionId);
                    }
                }
                else
                {
                    _lastError = result.FailureDetail;
                }

                RestoreCompleted?.Invoke(result);
                _events.Publish(new RestoreCompletedEvent(result));
                onComplete?.Invoke(result);
            };

            try
            {
                _provider.RestorePurchases(handleRestore);
            }
            catch (Exception exception)
            {
                HandleProviderException(exception, "RestorePurchases");
                handleRestore(new RestoreResult(false, Array.Empty<ProductId>(), "The purchase provider threw an exception."));
            }
        }

        public bool IsTransactionProcessed(string transactionId) =>
            !string.IsNullOrEmpty(transactionId) && _processedTransactionIds.Contains(transactionId);

        public PurchaseDiagnostics GetDiagnostics() =>
            new PurchaseDiagnostics(State, GetProducts(), _processedTransactionIds.Count, _lastError);

        public void Save()
        {
            var data = new PurchaseSaveData();
            data.ProcessedTransactionIds.AddRange(_processedTransactionIds);
            _persistence.Save(SaveKey, data, SaveVersion);
            _isDirty = false;
        }

        public void Load()
        {
            PurchaseSaveData data = _persistence.Load(SaveKey, SaveVersion, new PurchaseSaveData());
            _processedTransactionIds.Clear();
            if (data.ProcessedTransactionIds != null)
            {
                foreach (string id in data.ProcessedTransactionIds)
                {
                    // Post-load validation: skip entries ProcessGrant could never have written
                    // (empty/oversized) rather than trusting the file blindly.
                    if (!string.IsNullOrEmpty(id) && id.Length <= LocalPurchaseValidator.MaxTransactionIdLength)
                    {
                        _processedTransactionIds.Add(id);
                    }
                }
            }

            _isDirty = false;
        }

        private void HandleProviderResult(ProductDefinition definition, PurchaseResult result)
        {
            bool isTerminal = result.Kind != PurchaseResultKind.Pending && result.Kind != PurchaseResultKind.Deferred;

            Action<PurchaseResult> callback = null;
            if (isTerminal && _pendingCallbacks.TryGetValue(definition.Id.Value, out callback))
            {
                _pendingCallbacks.Remove(definition.Id.Value);
            }
            else if (!isTerminal)
            {
                _pendingCallbacks.TryGetValue(definition.Id.Value, out callback);
            }

            if (result.Kind == PurchaseResultKind.Success || result.Kind == PurchaseResultKind.Restored)
            {
                PurchaseValidationResult validation = _validator.Validate(result, definition);
                if (!validation.IsValid)
                {
                    var invalid = new PurchaseResult(PurchaseResultKind.ValidationFailed, result.ProductId, result.TransactionId, validation.FailureDetail);
                    RaiseFailed(invalid);
                    callback?.Invoke(invalid);
                    return;
                }

                ProcessGrant(definition, result, result.TransactionId);
                callback?.Invoke(result);
                return;
            }

            if (isTerminal)
            {
                RaiseFailed(result);
            }

            callback?.Invoke(result);
        }

        private void ProcessGrant(ProductDefinition definition, PurchaseResult result, string transactionId)
        {
            if (!string.IsNullOrEmpty(transactionId) && _processedTransactionIds.Contains(transactionId))
            {
                // Already granted for this exact transaction - a duplicate provider callback (see
                // CLAUDE.md's Phase 15 brief, section 41) reports the same outcome without granting again.
                RaiseCompleted(result);
                return;
            }

            EntitlementSource source = result.Kind == PurchaseResultKind.Restored ? EntitlementSource.Restored : EntitlementSource.Purchase;

            if (definition.GrantedEntitlementId.IsValid)
            {
                if (_entitlements != null)
                {
                    _entitlements.GrantEntitlement(definition.GrantedEntitlementId, source);
                }
                else
                {
                    _log?.Log(LogLevel.Warning, LogCategory,
                        $"Product '{definition.Id}' grants entitlement '{definition.GrantedEntitlementId}' but no IEntitlementService is registered.");
                }
            }

            if (definition.GrantedRewardId.IsValid)
            {
                if (_rewards != null)
                {
                    _rewards.TryClaim(definition.GrantedRewardId, "Purchase:" + definition.Id);
                }
                else
                {
                    _log?.Log(LogLevel.Warning, LogCategory,
                        $"Product '{definition.Id}' grants reward '{definition.GrantedRewardId}' but no IRewardService is registered.");
                }
            }

            if (!string.IsNullOrEmpty(transactionId))
            {
                _processedTransactionIds.Add(transactionId);
                _isDirty = true;
            }

            RaiseCompleted(result);
        }

        private void RaiseCompleted(PurchaseResult result)
        {
            PurchaseCompleted?.Invoke(result);
            _events.Publish(new PurchaseCompletedEvent(result));
        }

        private void RaiseFailed(PurchaseResult result)
        {
            _lastError = result.FailureDetail;
            PurchaseFailed?.Invoke(result);
            _events.Publish(new PurchaseFailedEvent(result));
        }

        private void OnProviderPurchaseUpdated(PurchaseResult result)
        {
            if (!_definitions.TryGetValue(result.ProductId.Value ?? string.Empty, out ProductDefinition definition))
            {
                _log?.Log(LogLevel.Warning, LogCategory, $"Provider reported an update for unknown product '{result.ProductId}'; ignored.");
                return;
            }

            HandleProviderResult(definition, result);
        }

        private void HandleProviderException(Exception exception, string operation)
        {
            _lastError = $"Purchase provider threw during {operation}.";
            _log?.Log(LogLevel.Error, LogCategory, $"{_lastError} {exception.GetType().Name}: {exception.Message}");
        }
    }
}
