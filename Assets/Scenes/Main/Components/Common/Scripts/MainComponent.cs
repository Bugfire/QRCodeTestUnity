using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MainComponents
{

    public class MainComponent : MonoBehaviour
    {
        [SerializeField]
        int CanvasWidth = 750;

        [SerializeField]
        int CanvasHeight = 1334;

        [SerializeField]
        Camera Camera = null;

        [SerializeField]
        Canvas[] Canvases = null;

        public bool Visible { get { return _isVisible; } }

        int _lastScreenWidth = 0;
        int _lastScreenHeight = 0;
        bool _isVisible = false;

        void Update()
        {
#if UNITY_EDITOR
            if (Application.isPlaying == false)
            {
                return;
            }
#endif
            AdjustCanvas();
            UpdateInternal();
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            var cameras = GetComponentsInChildren<Camera>();
            Camera = cameras[0];
            Canvases = GetComponentsInChildren<Canvas>(true);
            AdjustCanvas();
            OnValidateInternal();
        }
#endif

        public virtual void Open(int layer)
        {
            Camera.depth = layer * 100;
        }

        public virtual void Show()
        {
            _isVisible = true;
        }

        public virtual void Hide()
        {
            _isVisible = false;
        }

        public virtual void Close()
        {
        }

        public virtual void OnCloseButton()
        {
            MainBehaviours.ComponentManager.Instance.CloseComponent(this);
        }

        protected virtual void OnValidateInternal()
        {
        }

        protected virtual void UpdateInternal()
        {
        }

        public void AdjustCanvas(bool forceSetup = false)
        {
            var screenWidth = Screen.width;
            var screenHeight = Screen.height;
            if (Canvases == null ||
                (!forceSetup &&
                 _lastScreenWidth == screenWidth &&
                 _lastScreenHeight == screenHeight))
            {
                return;
            }
            _lastScreenWidth = screenWidth;
            _lastScreenHeight = screenHeight;
            var canvasAspect = (float)CanvasHeight / CanvasWidth;
            var screenAspect = (float)screenHeight / screenWidth;
            float scaleMin;
            if (canvasAspect < screenAspect)
            {
                scaleMin = 1f / (CanvasWidth * screenAspect);
            }
            else
            {
                scaleMin = 1f / CanvasHeight;
            }
            var defaultSizeDelta = new Vector2(CanvasWidth, CanvasHeight);
            for (var i = 0; i < Canvases.Length; i++)
            {
                var canvas = Canvases[i];
                RectTransform rt = canvas.transform as RectTransform;
                rt.anchorMin = new Vector2(0, 0);
                rt.anchorMax = new Vector2(0, 0);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = defaultSizeDelta;

                var scale = 35 * scaleMin;
                rt.localScale = new Vector3(scale, scale, 1f);
            }
        }
    }
}