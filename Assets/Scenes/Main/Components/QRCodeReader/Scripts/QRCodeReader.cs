using System.Threading;
using System.Collections.Generic;

using UnityEngine;

using ZXing;
using ZXing.QrCode;

namespace MainComponents
{
    public class QRCodeReader : MainComponent
    {
        [SerializeField]
        UnityEngine.UI.RawImage CameraImage = null;

        [SerializeField]
        UnityEngine.UI.Text MessageText = null;

        [SerializeField]
        GameObject FlipButton = null;

        [SerializeField]
        Animator ImageCanvasAnimator = null;

        [SerializeField]
        RectTransform Adjust = null;

        [SerializeField]
        QRCodeIndicator Indicator = null;

        public string LastResult { get; private set; }

        ResultPoint[] LastResultPoints = null;

        const string PrefsKeySelectedDevice = "QRCodeReader.SelectedDevice";

        string SelectedDevice
        {
            get
            {
                return PlayerPrefs.GetString(PrefsKeySelectedDevice, "");
            }
            set
            {
                PlayerPrefs.SetString(PrefsKeySelectedDevice, value);
            }
        }

        WebCamTexture CurrentWebCamTexture
        {
            get
            {
                if (WebCamTextures == null)
                {
                    return null;
                }
                if (CurrentCameraIndex < 0 || CurrentCameraIndex >= WebCamTextures.Length)
                {
                    return null;
                }
                return WebCamTextures[CurrentCameraIndex];
            }
        }
        WebCamTexture[] WebCamTextures;
        Thread DecodeThread;

        int CurrentCameraIndex;

        Color32[] CapturedBuffer;
        int CapturedWidth;
        int CapturedHeight;
        bool CapturedImageIsMirrored;

        bool isQuit;
        bool WebCamTexrureIsChanged;

        ManualResetEvent IsReady = new ManualResetEvent(false);
        ManualResetEvent IsParsing = new ManualResetEvent(false);
        ManualResetEvent IsDone = new ManualResetEvent(false);

        void OnEnable()
        {
            var c = CurrentWebCamTexture;
            if (c != null)
            {
                c.Play();
                CapturedWidth = c.width;
                CapturedHeight = c.height;
                WebCamTexrureIsChanged = false;
            }
        }

        void OnDisable()
        {
            var c = CurrentWebCamTexture;
            if (c != null)
            {
                c.Pause();
            }
        }

        void OnDestroy()
        {
            StopCamera();
        }

        void OnApplicationQuit()
        {
            isQuit = true;
        }

        public override void Open(int layer)
        {
            base.Open(layer);

            FlipButton.SetActive(false);
            LastResult = "";
            CurrentCameraIndex = -1;
        }

        public override void Show()
        {
            base.Show();
            StartCamera();
        }

        public override void Hide()
        {
            base.Hide();
            StopCamera();
        }

        public override void Close()
        {
            base.Close();
        }

        protected override void UpdateInternal()
        {
            if (CapturedBuffer == null)
            {
                if (string.IsNullOrEmpty(LastResult) == false && LastResult != MessageText.text)
                {
                    MessageText.text = LastResult;
                    ImageCanvasAnimator.PlayInFixedTime("Found", -1, 0);
                }
                var c = CurrentWebCamTexture;
                if (c != null)
                {
                    if (c.didUpdateThisFrame)
                    {
                        WebCamTexrureIsChanged = true;
                    }
                    if (IsDone.WaitOne(0) == true)
                    {
                        // get result
                        IsDone.Reset();
                        Indicator.SetPoints(CapturedWidth, CapturedHeight, LastResultPoints, CapturedImageIsMirrored);
                    }
                    if (WebCamTexrureIsChanged &&
                        IsReady.WaitOne(0) == false && IsParsing.WaitOne(0) == false && IsDone.WaitOne(0) == false)
                    {
                        WebCamTexrureIsChanged = false;
                        CapturedBuffer = c.GetPixels32();
                        CapturedWidth = c.width;
                        CapturedHeight = c.height;
                        CapturedImageIsMirrored = c.videoVerticallyMirrored;
                        IsReady.Set();
                    }
                }

                UpdateCameraImage();
            }
        }

        public void OnFlipButton()
        {
            if (WebCamTexture.devices.Length <= 1)
            {
                return;
            }
            var newCameraIndex = CurrentCameraIndex + 1;
            if (newCameraIndex >= WebCamTexture.devices.Length)
            {
                newCameraIndex = 0;
            }
            var c = CurrentWebCamTexture;
            if (c != null)
            {
                ImageCanvasAnimator.PlayInFixedTime("Flip", -1, 0);
            }
            StartCamera(newCameraIndex);
        }

        void StartCamera(int cameraIndex = -1)
        {
            var devices = WebCamTexture.devices;
            if (devices.Length == 0)
            {
                FlipButton.SetActive(false);
                return;
            }

            if (WebCamTextures == null)
            {
                WebCamTextures = new WebCamTexture[devices.Length];
            }

            if (WebCamTextures.Length == 1)
            {
                FlipButton.SetActive(false);
            }
            else
            {
                FlipButton.SetActive(true);
            }

            OnDisable();

            CurrentCameraIndex = 0;
            if (cameraIndex < 0)
            {
                var lastDevice = SelectedDevice;
                for (var i = 0; i < devices.Length; i++)
                {
                    if (devices[i].name == lastDevice)
                    {
                        CurrentCameraIndex = i;
                        break;
                    }
                }
            }
            else if (cameraIndex < devices.Length)
            {
                CurrentCameraIndex = cameraIndex;
            }

            // in iOS, "Front Camera", "Back Camera", videoRotationAngle = 90

            var c = CurrentWebCamTexture;
            if (c == null)
            {
                c = WebCamTextures[CurrentCameraIndex] = new WebCamTexture(
                    devices[CurrentCameraIndex].name,
                    640, 640,
                    Application.targetFrameRate);
            }
            OnEnable();
            if (c.isPlaying)
            {
                CameraImage.texture = c;
            }

            WebCamTexrureIsChanged = false;

            if (DecodeThread == null)
            {
                DecodeThread = new Thread(DecodeQR);
                DecodeThread.Start();
            }
        }

        void StopCamera()
        {
            if (DecodeThread != null)
            {
                DecodeThread.Abort();
                DecodeThread = null;
            }
            if (WebCamTextures != null)
            {
                for (var i = 0; i < WebCamTextures.Length; i++)
                {
                    var c = WebCamTextures[i];
                    if (c != null)
                    {
                        c.Stop();
                    }
                    WebCamTextures[i] = null;
                }
                WebCamTextures = null;
            }
            CameraImage.texture = null;
        }

        void UpdateCameraImage()
        {
            var c = CurrentWebCamTexture;
            if (c == null)
            {
                return;
            }
            if (c != CameraImage.texture)
            {
                if (c.isPlaying == false)
                {
                    return;
                }
                CameraImage.texture = c;
            }
            var imageWidth = c.width;
            var imageHeight = c.height;
            float u0, v0, u1, v1;
            if (imageWidth > imageHeight)
            {
                var skipWidth = (imageWidth - imageHeight) / (2f * imageWidth);
                u0 = skipWidth;
                v0 = 0;
                u1 = 1 - skipWidth;
                v1 = 1;
                CameraImage.uvRect = new Rect(skipWidth, 0, 1 - 2 * skipWidth, 1);
            }
            else
            {
                var skipHeight = (imageHeight - imageWidth) / (2f * imageHeight);
                u0 = 0;
                v0 = skipHeight;
                u1 = 1;
                v1 = 1 - skipHeight;
            }
            var vmirror = c.videoVerticallyMirrored;
            var hmirror = false;
            if (WebCamTexture.devices[CurrentCameraIndex].isFrontFacing)
            {
                hmirror = !hmirror;
            }
            if (c.videoRotationAngle == 90 || c.videoRotationAngle == 270)
            {
                var t = hmirror;
                hmirror = vmirror;
                vmirror = t;
            }
            CameraImage.uvRect = new Rect(u0, v0, u1 - u0, v1 - v0);
            Adjust.localRotation = Quaternion.AngleAxis(c.videoRotationAngle, Vector3.forward);
            Adjust.localScale = new Vector3(hmirror ? -1 : 1, vmirror ? -1 : 1, 1);

#if false
            Debug.LogFormat("XXX name:{0}  localRotation={1}, mirror={2} front={3}",
                            c.deviceName, c.videoRotationAngle, c.videoVerticallyMirrored, WebCamTexture.devices[CurrentCameraIndex].isFrontFacing);
#endif
        }

        void DecodeQR()
        {
            // create a reader with a custom luminance source
            var barcodeReader = new BarcodeReader();
            barcodeReader.Options.PossibleFormats = new List<BarcodeFormat>() { BarcodeFormat.QR_CODE };
            barcodeReader.Options.TryHarder = false;
            barcodeReader.AutoRotate = false;

            while (!isQuit)
            {
                try
                {
                    // decode the current frame
                    if (IsReady.WaitOne(1000))
                    {
                        if (IsParsing.WaitOne(0) == false)
                        {
                            IsParsing.Set();
                            IsReady.Reset();
                            var result = barcodeReader.Decode(CapturedBuffer, CapturedWidth, CapturedHeight);
                            CapturedBuffer = null;
                            if (result != null)
                            {
                                Debug.LogFormat("Points={0}", result.ResultPoints.Length);
                                LastResultPoints = result.ResultPoints;
                                LastResult = result.Text;
                            }
                            else
                            {
                                LastResultPoints = null;
                            }
                            IsDone.Set();
                            IsParsing.Reset();
                        }
                    }
                }
                catch
                {
                }
            }
        }
    }
}