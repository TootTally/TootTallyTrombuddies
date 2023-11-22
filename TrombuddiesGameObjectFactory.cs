﻿using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using TootTallyCore.APIServices;
using TootTallyCore.Graphics;
using TootTallyCore.Utils.Assets;
using UnityEngine;
using UnityEngine.UI;

namespace TootTallyTrombuddies
{
    public static class TrombuddiesGameObjectFactory
    {
        private static GameObject _userCardPrefab;

        [HarmonyPatch(typeof(GameObjectFactory), nameof(GameObjectFactory.OnHomeControllerInitialize))]
        [HarmonyPostfix]
        public static void InitializeTootTallySettingsManager(HomeController homeController)
        {
            SetUserCardPrefab();
            Plugin.Instance.gameObject.AddComponent<TrombuddiesManager>();
        }

        private static void SetUserCardPrefab()
        {
            _userCardPrefab = GameObject.Instantiate(GameObjectFactory.GetOverlayPanelPrefab.transform.Find("FSLatencyPanel").gameObject);
            _userCardPrefab.name = "UserCardPrefab";
            _userCardPrefab.GetComponent<Image>().color = new Color(0, 0, 0, 0);


            var fgRect = _userCardPrefab.transform.Find("LatencyFG").GetComponent<RectTransform>();
            var bgRect = _userCardPrefab.transform.Find("LatencyBG").GetComponent<RectTransform>();
            fgRect.GetComponent<Image>().maskable = bgRect.GetComponent<Image>().maskable = true;
            var size = new Vector2(360, 100);
            fgRect.sizeDelta = size;
            fgRect.anchoredPosition = Vector2.zero;
            bgRect.sizeDelta = size + (Vector2.one * 10f);
            bgRect.anchoredPosition = Vector2.zero;
            _userCardPrefab.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

            var horizontalContentHolder = fgRect.gameObject;
            GameObjectFactory.DestroyFromParent(horizontalContentHolder, "title");
            GameObjectFactory.DestroyFromParent(horizontalContentHolder, "subtitle");

            var horizontalLayoutGroup = horizontalContentHolder.AddComponent<HorizontalLayoutGroup>();
            horizontalLayoutGroup.padding = new RectOffset(5, 5, 5, 5);
            horizontalLayoutGroup.spacing = 20f;
            horizontalLayoutGroup.childAlignment = TextAnchor.MiddleCenter;
            horizontalLayoutGroup.childControlHeight = horizontalLayoutGroup.childControlWidth = true;
            horizontalLayoutGroup.childForceExpandHeight = horizontalLayoutGroup.childForceExpandWidth = false;



            var contentHolderLeft = horizontalContentHolder.transform.Find("MainPage").gameObject;
            contentHolderLeft.name = "LeftContent";

            var verticalLayoutGroup = contentHolderLeft.GetComponent<VerticalLayoutGroup>();
            verticalLayoutGroup.padding = new RectOffset(5, 5, 5, 5);
            verticalLayoutGroup.spacing = 4f;
            verticalLayoutGroup.childAlignment = TextAnchor.MiddleCenter;
            verticalLayoutGroup.childControlHeight = verticalLayoutGroup.childControlWidth = true;
            verticalLayoutGroup.childForceExpandHeight = verticalLayoutGroup.childForceExpandWidth = true;

            var contentHolderRight = GameObject.Instantiate(contentHolderLeft, horizontalContentHolder.transform);
            contentHolderRight.name = "RightContent";
            var verticalLayoutGroupRight = contentHolderRight.GetComponent<VerticalLayoutGroup>();
            verticalLayoutGroupRight.childControlHeight = verticalLayoutGroupRight.childControlWidth = false;
            verticalLayoutGroupRight.childForceExpandHeight = verticalLayoutGroupRight.childForceExpandWidth = false;

            var outlineTemp = new GameObject("PFPPrefab", typeof(Image));
            var outlineImage = outlineTemp.GetComponent<Image>();
            outlineImage.maskable = true;
            outlineImage.preserveAspect = true;

            var maskTemp = GameObject.Instantiate(outlineTemp, outlineTemp.transform);
            maskTemp.name = "ImageMask";
            var pfpTemp = GameObject.Instantiate(maskTemp, maskTemp.transform);
            pfpTemp.name = "Image";

            var mask = maskTemp.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            var maskImage = maskTemp.GetComponent<Image>();
            maskImage.sprite = AssetManager.GetSprite("PfpMask.png");
            maskTemp.GetComponent<RectTransform>().sizeDelta = new Vector2(90, 90);

            var pfpImage = pfpTemp.GetComponent<Image>();
            outlineTemp.transform.SetSiblingIndex(0);
            outlineImage.enabled = false;
            pfpImage.sprite = AssetManager.GetSprite("icon.png");
            pfpImage.preserveAspect = false;

            var layoutElement = outlineTemp.AddComponent<LayoutElement>();
            layoutElement.minHeight = layoutElement.minWidth = 96;
            var pfp = GameObject.Instantiate(outlineTemp, horizontalContentHolder.transform);
            pfp.transform.SetSiblingIndex(0);
            pfp.name = "PFP";
            GameObject.DestroyImmediate(outlineTemp);

            GameObject.DontDestroyOnLoad(_userCardPrefab);
            _userCardPrefab.SetActive(false);
        }

        public static GameObject CreateUserCard(Transform canvasTransform, SerializableClass.User user, string status)
        {
            GameObject card = GameObject.Instantiate(_userCardPrefab, canvasTransform);
            card.name = $"{user.username}UserCard";
            card.SetActive(true);

            var leftContent = card.transform.Find("LatencyFG/LeftContent").gameObject;

            var pfp = leftContent.transform.parent.Find("PFP/ImageMask/Image").GetComponent<Image>();
            if (user.picture != null)
                AssetManager.GetProfilePictureByID(user.id, sprite => pfp.sprite = sprite);


            var t1 = GameObjectFactory.CreateSingleText(leftContent.transform, "Name", $"{user.username}", Color.white);
            var t2 = GameObjectFactory.CreateSingleText(leftContent.transform, "Status", $"{status}", Color.white);
            t1.enableWordWrapping = t2.enableWordWrapping = false;
            t1.overflowMode = t2.overflowMode = TextOverflowModes.Ellipsis;

            var rightContent = card.transform.Find("LatencyFG/RightContent").gameObject;

            if (user.id != TootTallyAccounts.TootTallyUser.userInfo.id)
            {
                var bgColor = card.transform.Find("LatencyBG").GetComponent<Image>().color = UserFriendStatusToColor(user.friend_status);
                GameObjectFactory.TintImage(card.transform.Find("LatencyFG").GetComponent<Image>(), bgColor, .1f);
                if (user.friend_status == "Friend" || user.friend_status == "Mutuals")
                    GameObjectFactory.CreateCustomButton(rightContent.transform, Vector2.zero, new Vector2(30, 30), "-", "RemoveFriendButton", delegate { TrombuddiesManager.OnRemoveButtonPress(user); });
                else
                    GameObjectFactory.CreateCustomButton(rightContent.transform, Vector2.zero, new Vector2(30, 30), "+", "AddFriendButton", delegate { TrombuddiesManager.OnAddButtonPress(user); });
                GameObjectFactory.CreateCustomButton(rightContent.transform, Vector2.zero, new Vector2(30, 30), "P", "OpenProfileButton", delegate { TrombuddiesManager.OpenUserProfile(user.id); });
            }
            else
            {
                card.transform.Find("LatencyBG").GetComponent<Image>().color = Color.cyan;
                GameObjectFactory.TintImage(card.transform.Find("LatencyFG").GetComponent<Image>(), Color.cyan, .1f);
            }

            return card;
        }

        private static Color UserFriendStatusToColor(string status) =>
            status switch
            {
                "Friend" => new Color(0, .8f, 0, 1),
                "Mutuals" => new Color(1, 0, 1, 1),
                _ => new Color(0, 0, 0, 1),
            };
    }
}