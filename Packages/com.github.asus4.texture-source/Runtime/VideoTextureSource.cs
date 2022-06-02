namespace TextureSource
{
    using System.IO;
    using UnityEngine;
    using UnityEngine.Video;

    [CreateAssetMenu(menuName = "Texture Source/Video", fileName = "VideoTextureSource")]
    public class VideoTextureSource : BaseTextureSource
    {
        [SerializeField]
        [Tooltip("Whether to loop the video")]
        private bool loop = true;

        [SerializeField]
        [Tooltip("Whether to play sound in the video")]
        private bool playSound = false;

        [SerializeField]
        private string[] videoPaths;


        private VideoPlayer player;
        private int currentIndex;
        private long currentFrame = -1;

        public override bool DidUpdateThisFrame
        {
            get
            {
                long frame = player.frame;
                bool isUpdated = frame != currentFrame;
                currentFrame = frame;
                return isUpdated;
            }
        }

        public override Texture Texture => player.texture;

        public override void Start()
        {
            GameObject go = new GameObject(nameof(VideoTextureSource));
            player = go.AddComponent<VideoPlayer>();
            player.renderMode = VideoRenderMode.APIOnly;
            player.audioOutputMode = playSound
                ? VideoAudioOutputMode.Direct
                : VideoAudioOutputMode.None;
            player.isLooping = loop;

            currentIndex = Mathf.Min(currentIndex, videoPaths.Length - 1);

            StartVideo(currentIndex);
        }

        public override void Stop()
        {
            if (player == null)
            {
                return;
            }
            player.Stop();
            Destroy(player.gameObject);
            player = null;
        }

        public override void Next()
        {
            currentIndex++;
            if (currentIndex >= videoPaths.Length)
            {
                currentIndex = 0;
            }
            StartVideo(currentIndex);
        }

        private void StartVideo(int index)
        {
            string url = this.videoPaths[index];
            if (!Path.IsPathRooted(url))
            {
                url = Path.Combine(Application.dataPath, url);
            }

            Debug.Log($"Start video: {url}");
            player.url = url;
            player.Prepare();
            player.Play();

            currentFrame = -1;
        }
    }
}
