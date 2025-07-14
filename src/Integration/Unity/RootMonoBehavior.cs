using BitterCMS.Utility.Interfaces;
using UnityEngine;

namespace BitterCMS.UnityIntegration
{
    public abstract class RootMonoBehavior : MonoBehaviour, IRoot
    {
        protected abstract void GlobalStart();
        protected virtual void PreGlobalStart() { }
        protected virtual void FindExtraInteraction(Interaction interaction) { }
        
        void IRoot.PreStartGame()
        {
            PreInit(this, out var interaction);
            FindCoreInteraction(interaction);
            FindExtraInteraction(interaction);
            PreGlobalStart();
            GlobalStart();
        }

        void IRoot.UpdateGame(float timeDelta)
        {
            foreach (var element in InteractionCache<IEnterInUpdate>.AllInteraction)
            {
                element.Update(timeDelta);
            }
        }

        void IRoot.PhysicUpdateGame(float timeDelta)
        {
            foreach (var element in InteractionCache<IEnterInPhysicUpdate>.AllInteraction)
            {
                element.PhysicUpdate(timeDelta);
            }
        }

        void IRoot.LateUpdateGame(float timeDelta)
        {
            foreach (var element in InteractionCache<IEnterInLateUpdate>.AllInteraction)
            {
                element.LateUpdate(timeDelta);
            }
        }

        void IRoot.StoppedGame()
        {
            foreach (var element in InteractionCache<IExitInGame>.AllInteraction)
            {
                element.Stop();
            }
        }

        private void Awake()
        {
            ((IRoot)this).PreStartGame();
        }

        private void Update()
        {
            ((IRoot)this).UpdateGame(Time.deltaTime);
        }

        private void FixedUpdate()
        {
            ((IRoot)this).PhysicUpdateGame(Time.deltaTime);
        }

        private void LateUpdate()
        {
            ((IRoot)this).LateUpdateGame(Time.deltaTime);
        }

        private void OnDestroy()
        {
            ((IRoot)this).StoppedGame();
        }

        private static void PreInit(IRoot baseMain, out Interaction interaction)
        {
            interaction = new Interaction();
            interaction.Init();

            GlobalState.SetRoot(baseMain);
        }

        private void FindCoreInteraction(Interaction interaction)
        {

            var init = interaction.FindAll<IInitInRoot>();
            foreach (var element in init)
            {
                element.Init();
            }

            var starts = interaction.FindAll<IEnterInStart>();
            foreach (var element in starts)
            {
                element.Start();
            }

            interaction.FindAll<IEnterInUpdate>();
            interaction.FindAll<IEnterInPhysicUpdate>();
            interaction.FindAll<IEnterInLateUpdate>();

            interaction.FindAll<IColliderInteraction>();

            interaction.FindAll<IExitInGame>();
        }
    }
}
