using System.Threading;

using UnityEngine;

using ZXing;
using ZXing.QrCode;

namespace MainComponents
{
    public class QRCodeViewer : MainComponent
    {
        [SerializeField]
        UnityEngine.UI.RawImage CodeImage = null;

        [SerializeField]
        UnityEngine.UI.Text MessageText = null;

        Thread EncodeThread = null;

        bool IsEncoding = false;
        readonly ManualResetEvent IsDone = new ManualResetEvent(false);

        string EncodeMessage = null;
        const int EncodeWidth = 256;
        const int EncodeHeight = 256;
        Color32[] EncodedBuffer = null;
        Texture2D EncodedTexture = null;

        private void OnDestroy()
        {
            StopEncoding();
        }

        public override void Open(int layer)
        {
            base.Open(layer);
        }

        public override void Close()
        {
            base.Close();
            StopEncoding();
        }

        protected override void UpdateInternal()
        {
            if (IsEncoding && IsDone.WaitOne(0))
            {
                IsEncoding = false;
                EncodedTexture = new Texture2D(EncodeWidth, EncodeHeight);
                EncodedTexture.SetPixels32(EncodedBuffer);
                EncodedTexture.Apply();
                CodeImage.texture = EncodedTexture;
            }
        }

        public void Setup(string message)
        {
            IsEncoding = true;
            IsDone.Reset();
            EncodedTexture = null;
            EncodeMessage = message;
            MessageText.text = message;
            StartEncoding();
        }

        void StartEncoding()
        {
            EncodeThread = new Thread(Encode);
            EncodeThread.Start();
        }

        void StopEncoding()
        {
            if (EncodeThread != null)
            {
                EncodeThread.Abort();
                EncodeThread = null;
            }
        }

        void Encode()
        {
            var writer = new BarcodeWriter
            {
                Format = BarcodeFormat.QR_CODE,
                Options = new QrCodeEncodingOptions
                {
                    Height = EncodeHeight,
                    Width = EncodeWidth,
                }
            };
            EncodedBuffer = writer.Write(EncodeMessage);
            IsDone.Set();
        }
    }
}