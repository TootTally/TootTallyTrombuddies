using System;
using System.Collections.Generic;
using static TootTallyCore.APIServices.SerializableClass;
using TMPro;
using TootTallyAccounts;
using TootTallyCore.APIServices;
using TootTallyCore.Graphics;
using TootTallyCore.Utils.Assets;
using UnityEngine.UI;
using UnityEngine;
using TootTallyCore.Graphics.Animations;
using TootTallyCore.Utils.TootTallyNotifs;

namespace TootTallyTrombuddies
{
    public class TrombuddiesManager : MonoBehaviour
    {
        private static readonly List<KeyCode> _keyInputList = new() { Plugin.Instance.ToggleFriendOnly.Value, Plugin.Instance.ToggleOnlineOnly.Value, KeyCode.UpArrow, KeyCode.DownArrow, KeyCode.LeftArrow, KeyCode.RightArrow, KeyCode.B, KeyCode.A };
        private static readonly List<KeyCode> _kenoKeys = new List<KeyCode>{KeyCode.UpArrow, KeyCode.UpArrow,
                                       KeyCode.DownArrow, KeyCode.DownArrow,
                                       KeyCode.LeftArrow, KeyCode.RightArrow,
                                       KeyCode.LeftArrow, KeyCode.RightArrow,
                                       KeyCode.B, KeyCode.A};
        private static int _kenoIndex;
        public static bool IsPanelActive;
        private static bool _isInitialized;
        private static bool _isUpdating;
        private static GameObject _overlayCanvas;
        private static TootTallyAnimation _panelAnimationFG, _panelAnimationBG;

        private static GameObject _overlayPanel;
        private static GameObject _overlayPanelContainer;

        private static RectTransform _containerRect;
        private static TMP_Text _titleText;

        private static List<GameObject> _userObjectList;

        private static bool _showAllSUsers, _showFriends;

        private static float _scrollAcceleration;


        private void Awake()
        {
            if (_isInitialized) return;
            Initialize();
            TootTallyNotifManager.DisplayNotif("TromBuddies Panel Initialized!");
        }

        private static void Initialize()
        {
            _kenoIndex = 0;
            _overlayCanvas = new GameObject("TootTallyOverlayCanvas");
            GameObject.DontDestroyOnLoad(_overlayCanvas);

            Canvas canvas = _overlayCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 1;
            CanvasScaler scaler = _overlayCanvas.AddComponent<CanvasScaler>();
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _userObjectList = new List<GameObject>();


            _overlayPanel = GameObjectFactory.CreateOverlayPanel(_overlayCanvas.transform, Vector2.zero, new Vector2(1700, 900), 20f, "BonerBuddiesOverlayPanel");
            _overlayPanel.transform.Find("FSLatencyPanel/LatencyFG").GetComponent<Image>().color = new Color(.1f, .1f, .1f);
            _overlayPanelContainer = _overlayPanel.transform.Find("FSLatencyPanel/LatencyFG/MainPage").gameObject;
            _overlayPanel.transform.Find("FSLatencyPanel/LatencyFG").localScale = Vector2.zero;
            _overlayPanel.transform.Find("FSLatencyPanel/LatencyBG").localScale = Vector2.zero;
            _containerRect = _overlayPanelContainer.GetComponent<RectTransform>();
            _containerRect.anchoredPosition = Vector2.zero;
            _containerRect.sizeDelta = new Vector2(1700, 700);
            GameObject.DestroyImmediate(_overlayPanelContainer.GetComponent<VerticalLayoutGroup>());
            var gridLayoutGroup = _overlayPanelContainer.AddComponent<GridLayoutGroup>();
            gridLayoutGroup.padding = new RectOffset(20, 20, 20, 20);
            gridLayoutGroup.spacing = new Vector2(5, 5);
            gridLayoutGroup.cellSize = new Vector2(380, 120);
            gridLayoutGroup.childAlignment = TextAnchor.UpperCenter;
            _overlayPanelContainer.transform.parent.gameObject.AddComponent<Mask>();
            GameObjectFactory.DestroyFromParent(_overlayPanelContainer.transform.parent.gameObject, "subtitle");
            GameObjectFactory.DestroyFromParent(_overlayPanelContainer.transform.parent.gameObject, "title");
            var text = GameObjectFactory.CreateSingleText(_overlayPanelContainer.transform, "title", "TromBuddies");
            _titleText = text.GetComponent<TMP_Text>();
            var layoutElement = text.gameObject.AddComponent<LayoutElement>();
            layoutElement.ignoreLayout = true;
            text.raycastTarget = false;
            text.alignment = TMPro.TextAlignmentOptions.Top;
            text.rectTransform.anchoredPosition = new Vector2(0, 15);
            text.rectTransform.pivot = new Vector2(0, .5f);
            text.rectTransform.sizeDelta = new Vector2(1700, 800);
            text.fontSize = 60f;
            text.overflowMode = TMPro.TextOverflowModes.Ellipsis;
            GameObjectFactory.CreateCustomButton(_overlayPanelContainer.transform.parent, Vector2.zero, new Vector2(60, 60), AssetManager.GetSprite("Close64.png"), "CloseTromBuddiesButton", TogglePanel);

            _overlayPanel.SetActive(false);
            IsPanelActive = false;
            _isInitialized = true;
        }

        private void Update()
        {
            if (!_isInitialized) return;

            if (Input.GetKeyDown(Plugin.Instance.TogglePanel.Value))
            {
                UserStatusManager.ResetTimerAndWakeUpIfIdle();
                TogglePanel();
            }

            if (!IsPanelActive) return;

            _keyInputList.ForEach(key =>
            {
                if (Input.GetKeyDown(key))
                    HandleKeyDown(key);
            });

            if (Input.mouseScrollDelta.y != 0)
                AddScrollAcceleration(Input.mouseScrollDelta.y * 2f);
            UpdateScrolling();
        }

        private static void HandleKeyDown(KeyCode keypressed)
        {
            if (_isUpdating)
            {
                TootTallyNotifManager.DisplayNotif("Panel currently updating, be patient!");
                return;
            }

            if (Plugin.Instance.ToggleOnlineOnly.Value == keypressed)
            {
                _showAllSUsers = !_showAllSUsers;
                TootTallyNotifManager.DisplayNotif(_showAllSUsers ? "Showing all users" : "Showing online users");
                UpdateUsers();
            }
            else if (Plugin.Instance.ToggleFriendOnly.Value == keypressed)
            {
                _showFriends = !_showFriends;
                TootTallyNotifManager.DisplayNotif(_showFriends ? "Showing friends only" : "Showing non-friend users");
                UpdateUsers();
            }

            if (_kenoKeys.Contains(keypressed))
                if (_kenoIndex != -1 && _kenoKeys[_kenoIndex] == keypressed)
                {
                    _kenoIndex++;
                    if (_kenoIndex >= _kenoKeys.Count)
                        OnKenomiCodeEnter();
                }
                else
                    _kenoIndex = 0;
            else
                _kenoIndex = 0;
        }

        private static void OnKenomiCodeEnter()
        {
            _titleText.text = "BonerBuddies";
            TootTallyNotifManager.DisplayNotif("Secret found... ☠");
            _kenoIndex = -1;
        }

        private static void AddScrollAcceleration(float value)
        {
            _scrollAcceleration -= value / Time.deltaTime;
        }

        private static void UpdateScrolling()
        {
            _containerRect.anchoredPosition = new Vector2(_containerRect.anchoredPosition.x, Math.Max(_containerRect.anchoredPosition.y + (_scrollAcceleration * Time.deltaTime), 0));
            if (_containerRect.anchoredPosition.y <= 0)
                _scrollAcceleration = 0;
            else
                _scrollAcceleration -= (_scrollAcceleration * 10f) * Time.deltaTime; //Abitrary value just so it looks nice / feel nice
        }

        public static void TogglePanel()
        {
            IsPanelActive = !IsPanelActive;
            if (_overlayPanel != null)
            {
                _panelAnimationBG?.Dispose();
                _panelAnimationFG?.Dispose();
                var targetVector = IsPanelActive ? Vector2.one : Vector2.zero;
                var animationTime = IsPanelActive ? 1f : 0.45f;
                var secondDegreeAnimationFG = IsPanelActive ? new SecondDegreeDynamicsAnimation(1.75f, 1f, 0f) : new SecondDegreeDynamicsAnimation(3.2f, 1f, 0.25f);
                var secondDegreeAnimationBG = IsPanelActive ? new SecondDegreeDynamicsAnimation(1.75f, 1f, 0f) : new SecondDegreeDynamicsAnimation(3.2f, 1f, 0.25f);
                _panelAnimationFG = TootTallyAnimationManager.AddNewScaleAnimation(_overlayPanel.transform.Find("FSLatencyPanel/LatencyFG").gameObject, targetVector, animationTime, secondDegreeAnimationFG);
                _panelAnimationBG = TootTallyAnimationManager.AddNewScaleAnimation(_overlayPanel.transform.Find("FSLatencyPanel/LatencyBG").gameObject, targetVector, animationTime, secondDegreeAnimationBG, (sender) =>
                {
                    if (!IsPanelActive)
                        _overlayPanel.SetActive(IsPanelActive);
                });
                if (IsPanelActive)
                {
                    _overlayPanel.SetActive(IsPanelActive);
                    UpdateUsers();
                }
                else
                    ClearUsers();
            }
        }

        public static void UpdateUsers()
        {
            if (IsPanelActive && !_isUpdating && TootTallyUser.userInfo != null)
            {
                _isUpdating = true;
                if (_showFriends && _showAllSUsers)
                    Plugin.Instance.StartCoroutine(TootTallyAPIService.GetFriendList(TootTallyAccounts.Plugin.GetAPIKey, OnUpdateUsersResponse));
                else if (_showAllSUsers)
                    Plugin.Instance.StartCoroutine(TootTallyAPIService.GetAllUsersUpToPageID(TootTallyUser.userInfo.id, 3, OnUpdateUsersResponse));
                else if (_showFriends)
                    Plugin.Instance.StartCoroutine(TootTallyAPIService.GetOnlineFriends(TootTallyAccounts.Plugin.GetAPIKey, OnUpdateUsersResponse));
                else
                    Plugin.Instance.StartCoroutine(TootTallyAPIService.GetLatestOnlineUsers(TootTallyUser.userInfo.id, OnUpdateUsersResponse));
            }



        }

        private static void OnUpdateUsersResponse(List<User> users)
        {
            ClearUsers();
            users?.ForEach(user =>
            {
                _userObjectList.Add(TrombuddiesGameObjectFactory.CreateUserCard(_overlayPanelContainer.transform, user, GetStatusString(user)));
            });
            _isUpdating = false;
        }

        private static string GetStatusString(User user)
        {
            switch (user.status)
            {
                case "Offline":
                    return $"<size=16><color=red>{user.status}</color></size>";

                case "Idle":
                    return $"<size=16><color=yellow>{user.status}</color></size>";

                default:
                    if (user.currently_playing != null)
                        return $"<size=16><color=green>{user.status}\n{user.currently_playing[0].short_name}</color></size>";
                    else
                        return $"<size=16><color=green>{user.status}</color></size>";
            }
        }


        public static void OnAddButtonPress(User user) =>
            Plugin.Instance.StartCoroutine(TootTallyAPIService.AddFriend(TootTallyAccounts.Plugin.GetAPIKey, user.id, OnFriendResponse));
        public static void OnRemoveButtonPress(User user) =>
            Plugin.Instance.StartCoroutine(TootTallyAPIService.RemoveFriend(TootTallyAccounts.Plugin.GetAPIKey, user.id, OnFriendResponse));
        public static void OpenUserProfile(int id) => Application.OpenURL($"https://toottally.com/profile/{id}");

        private static void OnFriendResponse(bool value)
        {
            if (value)
                UpdateUsers();
            TootTallyNotifManager.DisplayNotif(value ? "Friend list updated." : "Action couldn't be done.");
        }

        public static void ClearUsers()
        {
            _userObjectList?.ForEach(DestroyImmediate);
            _userObjectList?.Clear();
        }

        public static void UpdateTheme()
        {
            if (!_isInitialized) return;
            Dispose();
            Initialize();
        }

        public static void Dispose()
        {
            if (!_isInitialized) return; //just in case too

            GameObject.DestroyImmediate(_overlayCanvas);
            GameObject.DestroyImmediate(_overlayPanel);
            _isInitialized = false;
        }
    }
}
