using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MainBehaviours
{

    public class ComponentManager : MonoBehaviourSingleton<ComponentManager>
    {
        [SerializeField]
        MainComponents.Background Background = null;

        [SerializeField]
        MainComponents.QRCodeReader QRCReader = null;

        [SerializeField]
        MainComponents.QRCodeViewer QRCViewer = null;

        [SerializeField]
        MainComponents.MainMenu MainMenu = null;

        [SerializeField]
        MainComponents.MainComponent[] Components = null;

        List<MainComponents.MainComponent> ActiveComponents = null;

        void Start()
        {
            ActiveComponents = new List<MainComponents.MainComponent>();
            for (var i = 0; i < Components.Length; i++)
            {
                Components[i].gameObject.SetActive(false);
            }
            Background.gameObject.SetActive(true);
            OpenMainMenu();
        }

        public void OpenMainMenu()
        {
            OpenComponent(MainMenu);
        }

        public void OpenQRCReader()
        {
            OpenComponent(QRCReader);
        }

        public void OpenQRCViewer(string message)
        {
            OpenComponent(QRCViewer);
            QRCViewer.Setup(message);
        }

        public void CloseComponent(MainComponents.MainComponent component)
        {
            if (!ActiveComponents.Contains(component))
            {
                Debug.LogError("Already inactive");
                return;
            }
            ActiveComponents.Remove(component);
            component.Close();
            component.gameObject.SetActive(false);

            if (ActiveComponents.Count > 0)
            {
                ActiveComponents[ActiveComponents.Count - 1].Show();
            }
        }

        void OpenComponent(MainComponents.MainComponent component)
        {
            if (ActiveComponents.Contains(component))
            {
                Debug.LogError("Already active");
                return;
            }

            var index = ActiveComponents.Count;
            ActiveComponents.Add(component);
            if (ActiveComponents.Count > 1)
            {
                ActiveComponents[ActiveComponents.Count - 2].Hide();
            }

            component.gameObject.SetActive(true);
            component.Open(index);
            component.Show();
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (gameObject.scene.isLoaded == false)
            {
                return;
            }
            var topNodes = gameObject.scene.GetRootGameObjects();
            var componentsList = new List<MainComponents.MainComponent>();
            foreach (var node in topNodes)
            {
                componentsList.AddRange(node.GetComponentsInChildren<MainComponents.MainComponent>(true));
            }
            Components = componentsList.ToArray();

            for (var i = 0; i < Components.Length; i++)
            {
                var t = Components[i];
                if (t is MainComponents.Background)
                {
                    Background = t as MainComponents.Background;
                }
                if (t is MainComponents.QRCodeReader)
                {
                    QRCReader = t as MainComponents.QRCodeReader;
                }
                if (t is MainComponents.QRCodeViewer)
                {
                    QRCViewer = t as MainComponents.QRCodeViewer;
                }
                if (t is MainComponents.MainMenu)
                {
                    MainMenu = t as MainComponents.MainMenu;
                }
            }
        }
#endif
    }
}