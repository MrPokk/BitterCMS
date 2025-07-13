using BitterCMS.CMSSystem;
using BitterCMS.Component;
using UnityEngine;

namespace BitterCMS.UnityIntegration
{
    [RequireComponent(typeof(Collider2D), typeof(Rigidbody2D))]
    public abstract class ViewCollision : CMSViewCore
    {
        private void OnCollisionEnter2D(Collision2D other)
        {
            var viewCollision = other.collider.gameObject.GetComponent<ViewCollision>();
            if (!viewCollision)
                return;

            GetModel().GetComponent<ColliderComponent>().OtherCollision = viewCollision;
            foreach (var interaction in InteractionCache<IColliderInteraction>.AllInteraction)
            {
                interaction.EnterCollider(GetComponent<ViewCollision>(), viewCollision);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var viewCollision = other.gameObject.GetComponent<ViewCollision>();
            if (!viewCollision)
                return;

            GetModel().GetComponent<ColliderComponent>().OtherCollision = viewCollision;
            foreach (var interaction in InteractionCache<IColliderInteraction>.AllInteraction)
            {
                interaction.EnterCollider(GetComponent<ViewCollision>(), viewCollision);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            var viewCollision = other.gameObject.GetComponent<ViewCollision>();
            if (!viewCollision)
                return;

            GetModel().GetComponent<ColliderComponent>().OtherCollision = null;
            foreach (var interaction in InteractionCache<IColliderInteraction>.AllInteraction)
            {
                interaction.ExitCollider(GetComponent<ViewCollision>(), viewCollision);
            }
        }

        private void OnCollisionExit2D(Collision2D other)
        {
            var viewCollision = other.collider.gameObject.GetComponent<ViewCollision>();
            if (!viewCollision)
                return;

            GetModel().GetComponent<ColliderComponent>().OtherCollision = null;
            foreach (var interaction in InteractionCache<IColliderInteraction>.AllInteraction)
            {
                interaction.ExitCollider(GetComponent<ViewCollision>(), viewCollision);
            }
        }
    }
}
