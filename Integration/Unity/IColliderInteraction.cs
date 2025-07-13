namespace BitterCMS.UnityIntegration
{
    public interface IColliderInteraction
    {
        public void UpdateCollider(ViewCollision source, ViewCollision collision) { }
        public void EnterCollider(ViewCollision source, ViewCollision collision) { }
        public void ExitCollider(ViewCollision source, ViewCollision collision) { }
    }
}
