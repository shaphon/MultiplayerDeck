using UnityEngine;
using Steamworks;

namespace MultiplayerDeck
{
    public class PlayerNameTag : MonoBehaviour
    {
        public ulong SteamId;

        private SpriteRenderer _avatarRenderer;
        private TextMesh _nameText;
        private int _avatarHandle = -1;
        private bool _avatarLoaded = false;
        private Camera _gameCamera;
        private Transform _parentTransform;
        private SpriteRenderer _bubbleSprite;

        private const float MaxNameWidth = 3f;
        private const float MinFontSize = 12f;
        private const float MaxFontSize = 28f;
        private const float AvatarSize = 0.3f;
        private const float AvatarNameGap = 0.15f;

        private void Start()
        {
            Transform avatarTransform = transform.Find("Avatar");
            Transform nameTransform = transform.Find("NameText");
            
            if (avatarTransform == null || nameTransform == null)
            {
                Debug.LogError("[NameTag] Avatar or NameText not found for SteamID: " + SteamId);
                return;
            }
            
            _avatarRenderer = avatarTransform.GetComponent<SpriteRenderer>();
            _nameText = nameTransform.GetComponent<TextMesh>();
            _parentTransform = transform.parent;

            if (_avatarRenderer == null || _nameText == null)
            {
                Debug.LogError("[NameTag] Required components not found for SteamID: " + SteamId);
                return;
            }

            if (FieldSystem.instance != null && FieldSystem.instance.MainCamera != null)
            {
                _gameCamera = FieldSystem.instance.MainCamera;
            }
            else
            {
                _gameCamera = Camera.main;
            }

            if (_parentTransform != null)
            {
                PlayerController parentController = _parentTransform.GetComponent<PlayerController>();
                if (parentController != null && parentController.Emoji != null && parentController.Emoji.EmojiSprite != null)
                {
                    _bubbleSprite = parentController.Emoji.EmojiSprite;

                    _avatarRenderer.sortingLayerName = _bubbleSprite.sortingLayerName;
                    _avatarRenderer.sortingOrder = _bubbleSprite.sortingOrder + 1;

                    Renderer nameRenderer = _nameText.GetComponent<Renderer>();
                    if (nameRenderer != null)
                    {
                        nameRenderer.sortingLayerName = _bubbleSprite.sortingLayerName;
                        nameRenderer.sortingOrder = _bubbleSprite.sortingOrder + 2;
                    }

                    _avatarRenderer.material = _bubbleSprite.material;
                }
            }

            CSteamID sid = new CSteamID(SteamId);
            string name = SteamFriends.GetFriendPersonaName(sid);

            if (string.IsNullOrEmpty(name) || name == "Unknown")
            {
                CSteamID myId = SteamUser.GetSteamID();
                name = SteamFriends.GetFriendPersonaName(myId);
                if (string.IsNullOrEmpty(name)) name = "Player";
                name += "（测试）";

                int myAvatar = SteamFriends.GetLargeFriendAvatar(myId);
                if (myAvatar != -1)
                {
                    _avatarHandle = myAvatar;
                    LoadAvatarTexture();
                }
            }
            else
            {
                _avatarHandle = SteamFriends.GetLargeFriendAvatar(sid);
                if (_avatarHandle != -1)
                {
                    LoadAvatarTexture();
                }
            }

            _nameText.text = name;
            AdjustNameTextSize();
            LayoutAvatarAndName();
        }

        private void AdjustNameTextSize()
        {
            string name = _nameText.text;
            if (string.IsNullOrEmpty(name)) return;

            float fontSize = MaxFontSize;
            _nameText.fontSize = (int)fontSize;

            float estimatedWidth = name.Length * _nameText.characterSize * fontSize * 0.05f;

            while (estimatedWidth > MaxNameWidth && fontSize > MinFontSize)
            {
                fontSize -= 1f;
                _nameText.fontSize = (int)fontSize;
                estimatedWidth = name.Length * _nameText.characterSize * fontSize * 0.05f;
            }

            _nameText.characterSize = 0.08f * (fontSize / MaxFontSize);
        }

        private void LayoutAvatarAndName()
        {
            _avatarRenderer.transform.localScale = new Vector3(AvatarSize, AvatarSize, 1f);

            _nameText.anchor = TextAnchor.MiddleLeft;
            _nameText.alignment = TextAlignment.Left;

            float nameWidth = _nameText.text.Length * _nameText.characterSize * _nameText.fontSize * 0.05f;
            float totalWidth = AvatarSize + AvatarNameGap + nameWidth;
            float halfWidth = totalWidth * 0.5f;

            _avatarRenderer.transform.localPosition = new Vector3(-halfWidth, 0f, 0f);

            float nameOffsetX = -halfWidth + AvatarSize + AvatarNameGap;
            _nameText.transform.localPosition = new Vector3(nameOffsetX, 0f, 0f);
        }

        private void LateUpdate()
        {
            if (_gameCamera != null)
            {
                transform.LookAt(_gameCamera.transform);
                transform.Rotate(0f, 180f, 0f);
            }

            if (!_avatarLoaded && _avatarHandle == -1)
            {
                int currentHandle = SteamFriends.GetLargeFriendAvatar(new CSteamID(SteamId));
                if (currentHandle != -1)
                {
                    _avatarHandle = currentHandle;
                    LoadAvatarTexture();
                }
            }
        }

        private void LoadAvatarTexture()
        {
            if (SteamUtils.GetImageSize(_avatarHandle, out uint width, out uint height))
            {
                byte[] rgba = new byte[width * height * 4];
                if (SteamUtils.GetImageRGBA(_avatarHandle, rgba, rgba.Length))
                {
                    Texture2D tex = new Texture2D((int)width, (int)height, TextureFormat.RGBA32, false);
                    tex.LoadRawTextureData(rgba);
                    tex.Apply();

                    Color32[] pixels = tex.GetPixels32();
                    int w = (int)width;
                    int h = (int)height;
                    for (int y = 0; y < h / 2; y++)
                    {
                        for (int x = 0; x < w; x++)
                        {
                            int topIndex = y * w + x;
                            int bottomIndex = (h - 1 - y) * w + x;
                            Color32 temp = pixels[topIndex];
                            pixels[topIndex] = pixels[bottomIndex];
                            pixels[bottomIndex] = temp;
                        }
                    }
                    tex.SetPixels32(pixels);
                    tex.Apply();

                    Sprite sprite = Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
                    _avatarRenderer.sprite = sprite;
                    _avatarRenderer.color = Color.white;
                    _avatarLoaded = true;
                }
            }
        }
    }
}
