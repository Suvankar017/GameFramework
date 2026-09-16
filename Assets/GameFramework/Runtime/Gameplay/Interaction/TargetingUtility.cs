using UnityEngine;

namespace GameFramework.Gameplay.Interaction
{
    /// <summary>
    /// Small, allocation-conscious targeting helpers — not an AI targeting system. Physics query
    /// methods take a caller-owned results buffer and layer mask explicitly rather than allocating
    /// one internally, so a caller doing this every frame controls its own allocation and buffer
    /// size. 3D (<see cref="Collider"/>) and 2D (<see cref="Collider2D"/>) variants are both
    /// provided since either may be in use depending on the game.
    /// </summary>
    public static class TargetingUtility
    {
        /// <summary>Non-allocating 3D overlap query. Returns the number of results written into
        /// <paramref name="resultsBuffer"/> (up to its length) — size the buffer for the maximum
        /// number of targets you actually care about.</summary>
        public static int FindTargetsInRadius(Vector3 origin, float radius, LayerMask layerMask, Collider[] resultsBuffer)
        {
            return Physics.OverlapSphereNonAlloc(origin, radius, resultsBuffer, layerMask);
        }

        /// <summary>Non-allocating 2D overlap query — see <see cref="FindTargetsInRadius"/>.</summary>
        public static int FindTargetsInRadius2D(Vector2 origin, float radius, LayerMask layerMask, Collider2D[] resultsBuffer)
        {
            return Physics2D.OverlapCircleNonAlloc(origin, radius, resultsBuffer, layerMask);
        }

        /// <summary>Returns the closest of the first <paramref name="count"/> entries in
        /// <paramref name="candidates"/> (as filled by <see cref="FindTargetsInRadius"/>), or null
        /// if <paramref name="count"/> is 0.</summary>
        public static Transform GetClosest(Vector3 origin, Collider[] candidates, int count)
        {
            Transform closest = null;
            float closestSqrDistance = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                if (candidates[i] == null)
                {
                    continue;
                }

                float sqrDistance = (candidates[i].transform.position - origin).sqrMagnitude;
                if (sqrDistance < closestSqrDistance)
                {
                    closestSqrDistance = sqrDistance;
                    closest = candidates[i].transform;
                }
            }

            return closest;
        }

        /// <summary>2D counterpart of <see cref="GetClosest"/>.</summary>
        public static Transform GetClosest2D(Vector2 origin, Collider2D[] candidates, int count)
        {
            Transform closest = null;
            float closestSqrDistance = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                if (candidates[i] == null)
                {
                    continue;
                }

                float sqrDistance = ((Vector2)candidates[i].transform.position - origin).sqrMagnitude;
                if (sqrDistance < closestSqrDistance)
                {
                    closestSqrDistance = sqrDistance;
                    closest = candidates[i].transform;
                }
            }

            return closest;
        }

        /// <summary>True if <paramref name="targetPosition"/> is within
        /// <paramref name="maxAngleDegrees"/> of <paramref name="forward"/>, measured from
        /// <paramref name="origin"/>. A cheap dot-product cone test — do this before an expensive
        /// physics query to cull candidates, not after.</summary>
        public static bool IsWithinViewCone(Vector3 origin, Vector3 forward, Vector3 targetPosition, float maxAngleDegrees)
        {
            Vector3 toTarget = targetPosition - origin;
            if (toTarget.sqrMagnitude < 0.0001f)
            {
                return true;
            }

            float angle = Vector3.Angle(forward, toTarget);
            return angle <= maxAngleDegrees;
        }
    }
}
