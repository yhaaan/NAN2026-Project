using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace NAN2026.Gomoku.Tests
{
    public sealed class GomokuMvpPlayModeTests
    {
        [UnityTest]
        public IEnumerator ShopHoverShowsUnitAndRoleDescriptions()
        {
            AsyncOperation load = SceneManager.LoadSceneAsync("GomokuMvp", LoadSceneMode.Single);
            while (!load.isDone)
            {
                yield return null;
            }

            yield return null;

            FieldInfo boundDefinitionField = typeof(ShopSlotView).GetField(
                "boundDefinition",
                BindingFlags.Instance | BindingFlags.NonPublic);
            ShopSlotView hoveredSlot = null;
            UnitDefinitionSO hoveredDefinition = null;
            float deadline = Time.realtimeSinceStartup + 5f;

            while (Time.realtimeSinceStartup < deadline && hoveredDefinition == null)
            {
                ShopSlotView[] slots = Object.FindObjectsByType<ShopSlotView>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
                foreach (ShopSlotView slot in slots)
                {
                    if (!slot.gameObject.activeInHierarchy)
                    {
                        continue;
                    }

                    hoveredDefinition =
                        boundDefinitionField.GetValue(slot) as UnitDefinitionSO;
                    if (hoveredDefinition != null)
                    {
                        hoveredSlot = slot;
                        break;
                    }
                }

                yield return null;
            }

            Assert.That(hoveredSlot, Is.Not.Null);
            Assert.That(hoveredDefinition, Is.Not.Null);

            UnitInfoPanelView infoPanel = Object.FindFirstObjectByType<UnitInfoPanelView>(
                FindObjectsInactive.Include);
            hoveredSlot.OnPointerEnter(null);
            yield return new WaitForSecondsRealtime(0.25f);

            TMP_Text name = infoPanel.transform.Find("Name").GetComponent<TMP_Text>();
            TMP_Text abilityName = infoPanel.transform.Find(
                "UnitDescriptionPanel/AbilityName").GetComponent<TMP_Text>();
            TMP_Text unitDescription = infoPanel.transform.Find(
                "UnitDescriptionPanel/UnitDescription").GetComponent<TMP_Text>();
            TMP_Text roleName = infoPanel.transform.Find(
                "RoleAbilityPanel/RoleName").GetComponent<TMP_Text>();
            TMP_Text roleDescription = infoPanel.transform.Find(
                "RoleAbilityPanel/RoleDescription").GetComponent<TMP_Text>();

            Assert.That(infoPanel.IsVisible, Is.True);
            Assert.That(name.text, Is.EqualTo(hoveredDefinition.DisplayName));
            Assert.That(abilityName.text, Is.Not.Empty);
            Assert.That(unitDescription.text, Is.EqualTo(hoveredDefinition.Description));
            Assert.That(roleName.text, Does.Contain(hoveredDefinition.RoleDisplayName));
            Assert.That(roleDescription.text, Is.EqualTo(hoveredDefinition.RoleDescription));

            hoveredSlot.OnPointerExit(null);
        }
        [UnityTest]
        public IEnumerator SceneStartsWithControllerAndFiveShopSlots()
        {
            AsyncOperation load = SceneManager.LoadSceneAsync("GomokuMvp", LoadSceneMode.Single);
            while (!load.isDone)
            {
                yield return null;
            }

            yield return null;

            GomokuGameController controller = Object.FindFirstObjectByType<GomokuGameController>();
            GomokuHud hud = Object.FindFirstObjectByType<GomokuHud>();
            GomokuBoardView boardView = Object.FindFirstObjectByType<GomokuBoardView>();
            PlacementCursorView placementCursor = Object.FindFirstObjectByType<PlacementCursorView>();
            ShopSlotView[] shopSlots = Object.FindObjectsByType<ShopSlotView>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            UnitInfoPanelView infoPanel = Object.FindFirstObjectByType<UnitInfoPanelView>(
                FindObjectsInactive.Include);
            TurnStatusView turnStatusView = Object.FindFirstObjectByType<TurnStatusView>(
                FindObjectsInactive.Include);
            AnimatedPauseMenuController pauseMenu = Object.FindFirstObjectByType<AnimatedPauseMenuController>(
                FindObjectsInactive.Include);

            UiButtonSfxFeedback uiButtonFeedback = Object.FindFirstObjectByType<UiButtonSfxFeedback>(
                FindObjectsInactive.Include);

            Assert.That(controller, Is.Not.Null);
            Assert.That(controller.enabled, Is.True);
            Assert.That(controller.PlayerSide, Is.EqualTo(StoneColor.White));
            Assert.That(controller.PrepareMusic.name, Is.EqualTo("prepare"));
            Assert.That(controller.BattleMusic.name, Is.EqualTo("battle"));
            Assert.That(controller.MusicFadeDuration, Is.GreaterThan(0f));
            Assert.That(SoundManager.Instance.CurrentMusic, Is.EqualTo(controller.PrepareMusic));
            Assert.That(SoundManager.Instance.IsMusicPlaying, Is.True);
            Assert.That(SoundManager.Instance.IsMusicLooping, Is.True);
            Assert.That(hud, Is.Not.Null);
            Assert.That(boardView, Is.Not.Null);
            Assert.That(boardView.WorldView, Is.Not.Null);
            Assert.That(boardView.HitSfx.Count, Is.EqualTo(4));
            Assert.That(boardView.HitSfx.Select(clip => clip != null ? clip.name : string.Empty),
                Is.EquivalentTo(new[] { "pop_1", "pop_2", "pop_3", "pop_4" }));
            Assert.That(SoundManager.Instance.MaxConcurrentSfxPerGroup, Is.EqualTo(2));
            SoundManager.Instance.StopAllSfx();
            AudioSource firstPopSource = SoundManager.Instance.PlaySfx(boardView.HitSfx[0]);
            AudioSource secondPopSource = SoundManager.Instance.PlaySfx(boardView.HitSfx[1]);
            AudioSource limitedPopSource = SoundManager.Instance.PlaySfx(boardView.HitSfx[2]);
            Assert.That(firstPopSource, Is.Not.Null);
            Assert.That(secondPopSource, Is.Not.Null);
            Assert.That(limitedPopSource, Is.Null);
            SoundManager.Instance.StopAllSfx();
            for (int index = 0; index < 5; index++)
            {
                AudioSource bypassedSource = SoundManager.Instance.PlaySfx(
                    boardView.HitSfx[0],
                    bypassConcurrencyLimit: true);
                Assert.That(bypassedSource, Is.Not.Null);
            }
            SoundManager.Instance.StopAllSfx();
            Assert.That(placementCursor, Is.Not.Null);
            Assert.That(shopSlots, Has.Length.EqualTo(ShopState.SlotCount));

            Assert.That(infoPanel, Is.Not.Null);
            Assert.That(infoPanel.IsVisible, Is.False);
            Assert.That(turnStatusView, Is.Not.Null);
            Assert.That(pauseMenu, Is.Not.Null);
            Assert.That(pauseMenu.MasterVolumeSlider, Is.Not.Null);
            Assert.That(pauseMenu.GetComponentsInChildren<Text>(true), Is.Empty);
            TMP_Text[] pauseMenuTexts = pauseMenu.GetComponentsInChildren<TMP_Text>(true);
            Assert.That(pauseMenuTexts, Has.Length.EqualTo(5));
            Assert.That(pauseMenuTexts.All(text => text.font.name.StartsWith("Maplestory Bold")), Is.True);

            Transform resultPanelRoot = hud.transform.Find("ResultPanel");
            Assert.That(resultPanelRoot, Is.Not.Null);
            Assert.That(resultPanelRoot.GetComponentsInChildren<Text>(true), Is.Empty);
            TMP_Text[] resultPanelTexts = resultPanelRoot.GetComponentsInChildren<TMP_Text>(true);
            Assert.That(resultPanelTexts, Has.Length.EqualTo(3));
            Assert.That(resultPanelTexts.All(text => text.font.name.StartsWith("Maplestory Bold")), Is.True);

            Assert.That(uiButtonFeedback, Is.Not.Null);
            Assert.That(uiButtonFeedback.ClickSfx.name, Is.EqualTo("drop_002"));
            Assert.That(uiButtonFeedback.PitchRange, Is.EqualTo(new Vector2(0.92f, 1.08f)));
            uiButtonFeedback.BindButtons();
            Assert.That(uiButtonFeedback.BoundButtonCount, Is.EqualTo(7));
            Assert.That(UiButtonSfxFeedback.CombatSpeedPitch(1), Is.EqualTo(0.84f).Within(0.001f));
            Assert.That(UiButtonSfxFeedback.CombatSpeedPitch(2), Is.EqualTo(0.92f).Within(0.001f));
            Assert.That(UiButtonSfxFeedback.CombatSpeedPitch(3), Is.EqualTo(1f).Within(0.001f));
            Assert.That(UiButtonSfxFeedback.CombatSpeedPitch(4), Is.EqualTo(1.08f).Within(0.001f));
            Assert.That(UiButtonSfxFeedback.CombatSpeedPitch(5), Is.EqualTo(1.16f).Within(0.001f));
            Assert.That(controller.VictorySfx.name, Is.EqualTo("harp strum 5"));
            Assert.That(controller.DefeatSfx.name, Is.EqualTo("wind down 2"));

            SoundManager.Instance.StopAllSfx();
            InvokePrivate(controller, "PlayResultSfx", true);
            AudioSource victorySource = FindConfiguredSfxSource();
            Assert.That(victorySource.clip.name, Is.EqualTo("harp strum 5"));
            SoundManager.Instance.StopAllSfx();
            InvokePrivate(controller, "PlayResultSfx", false);
            AudioSource defeatSource = FindConfiguredSfxSource();
            Assert.That(defeatSource.clip.name, Is.EqualTo("wind down 2"));
            SoundManager.Instance.StopAllSfx();

            float initialMasterVolume = SoundManager.Instance.MasterVolume;
            pauseMenu.MasterVolumeSlider.value = 0.37f;
            Assert.That(SoundManager.Instance.MasterVolume, Is.EqualTo(0.37f).Within(0.001f));
            pauseMenu.MasterVolumeSlider.value = initialMasterVolume;

            TMP_Text turnText = turnStatusView.transform.Find("TurnText").GetComponent<TMP_Text>();
            TMP_Text phaseText = turnStatusView.transform.Find("PhaseText").GetComponent<TMP_Text>();
            TMP_Text scoreText = turnStatusView.transform.Find("ScoreText").GetComponent<TMP_Text>();
            Slider combatSlider = turnStatusView.GetComponentInChildren<Slider>(true);
            Transform speedButtonRoot = hud.transform.Find("CombatSpeedPanel");
            Button speedButton = speedButtonRoot.GetComponent<Button>();
            Text speedText = speedButtonRoot.Find("SpeedText").GetComponent<Text>();
            Assert.That(turnText.text, Is.EqualTo("1턴"));
            Assert.That(phaseText.text, Is.EqualTo("적 턴"));
            Assert.That(scoreText.text, Is.EqualTo("0 : 0"));
            Assert.That(combatSlider.gameObject.activeSelf, Is.False);
            Assert.That(speedButtonRoot.gameObject.activeSelf, Is.True);
            Assert.That(speedText.text, Is.EqualTo("x1"));

            SoundManager.Instance.StopAllSfx();
            speedButton.onClick.Invoke();
            Assert.That(speedText.text, Is.EqualTo("x2"));
            AudioSource buttonSource = FindConfiguredSfxSource();
            Assert.That(buttonSource.clip.name, Is.EqualTo("drop_002"));
            Assert.That(
                buttonSource.pitch,
                Is.EqualTo(UiButtonSfxFeedback.CombatSpeedPitch(2)).Within(0.001f));
            speedButton.onClick.Invoke();
            Assert.That(speedText.text, Is.EqualTo("x3"));
            speedButton.onClick.Invoke();
            Assert.That(speedText.text, Is.EqualTo("x4"));
            speedButton.onClick.Invoke();
            Assert.That(speedText.text, Is.EqualTo("x5"));
            speedButton.onClick.Invoke();
            Assert.That(speedText.text, Is.EqualTo("x1"));
            speedButton.onClick.Invoke();
            Assert.That(speedText.text, Is.EqualTo("x2"));
            Assert.That(Time.timeScale, Is.EqualTo(1f));

            var pointer = new PointerEventData(EventSystem.current)
            {
                position = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f)
            };
            var raycastResults = new List<RaycastResult>();
            Object.FindFirstObjectByType<GraphicRaycaster>().Raycast(pointer, raycastResults);
            Assert.That(raycastResults, Is.Not.Empty);

            boardView.ShowDamage(7, 7, 25, true);
            yield return null;
            Assert.That(FindDirectChild(boardView.transform, "AttackDamagePopup(Clone)"), Is.Not.Null);
            Assert.That(
                SoundManager.Instance.GetComponentsInChildren<AudioSource>()
                    .Any(source => source.clip != null && source.clip.name.StartsWith("pop_")),
                Is.True);

            boardView.ShowDamage(7, 7, 25, false);
            yield return null;
            Assert.That(FindDirectChild(boardView.transform, "HitDamagePopup(Clone)"), Is.Not.Null);

            boardView.ShowHeal(7, 7, 15);
            yield return null;
            Assert.That(FindDirectChild(boardView.transform, "HealPopup(Clone)"), Is.Not.Null);

            yield return new WaitForSeconds(0.6f);
            Assert.That(phaseText.text, Is.EqualTo("플레이어 턴"));
            Assert.That(UnitLabels.GradeTextColor(UnitGrade.Common), Is.EqualTo(Color.black));
            foreach (ShopSlotView shopSlot in shopSlots)
            {
                Assert.That(shopSlot.ClickSfx, Is.Not.Null);
                Assert.That(shopSlot.ClickSfx.name, Is.EqualTo("card_draw_3"));
                Assert.That(shopSlot.HoverSfx, Is.Not.Null);
                Assert.That(shopSlot.HoverSfx.name, Is.EqualTo("drop_002"));
                TMP_Text unitLabel = shopSlot.transform.Find("Name").GetComponent<TMP_Text>();
                TMP_Text gradeLabel = shopSlot.transform.Find("Grade").GetComponent<TMP_Text>();
                TMP_Text statsLabel = shopSlot.transform.Find("Stats").GetComponent<TMP_Text>();
                Image cardBackground = shopSlot.GetComponent<Image>();
                Image roleIcon = shopSlot.transform.Find("RoleIcon").GetComponent<Image>();
                RectTransform cardRect = shopSlot.transform as RectTransform;

                Assert.That(unitLabel.text, Does.StartWith("<b>"));
                Assert.That(unitLabel.text, Does.Not.Contain("\n"));
                Assert.That(unitLabel.text.Replace("<b>", string.Empty).Replace("</b>", string.Empty).Replace(" ", string.Empty).Length, Is.LessThanOrEqualTo(5));
                Assert.That(unitLabel.fontSize, Is.EqualTo(20f));
                Assert.That(unitLabel.font.name, Does.StartWith("Maplestory Bold"));
                Assert.That(unitLabel.alignment, Is.EqualTo(TextAlignmentOptions.Left));
                Assert.That(unitLabel.rectTransform.anchoredPosition.x, Is.EqualTo(47.2f).Within(0.01f));
                Assert.That(unitLabel.rectTransform.anchoredPosition.y, Is.EqualTo(-18.6f).Within(0.01f));
                Assert.That(gradeLabel.text, Is.Not.Empty);
                Assert.That(gradeLabel.fontSize, Is.EqualTo(9f));
                Assert.That(gradeLabel.font.name, Does.StartWith("Maplestory Light"));
                Assert.That(gradeLabel.alignment, Is.EqualTo(TextAlignmentOptions.Right));
                Assert.That(statsLabel.alignment, Is.EqualTo(TextAlignmentOptions.Left));
                Assert.That(statsLabel.fontSize, Is.EqualTo(11f));
                Assert.That(statsLabel.font.name, Does.StartWith("Maplestory Light"));
                TMP_Text abilityNameLabel = shopSlot.transform.Find("AbilityName").GetComponent<TMP_Text>();
                TMP_Text abilityLabel = shopSlot.transform.Find("Ability").GetComponent<TMP_Text>();
                Assert.That(abilityNameLabel.fontSize, Is.EqualTo(11f));
                Assert.That(abilityNameLabel.font.name, Does.StartWith("Maplestory Bold"));
                Assert.That(abilityNameLabel.rectTransform.offsetMin.x, Is.EqualTo(12f).Within(0.01f));
                Assert.That(abilityLabel.fontSize, Is.EqualTo(10f));
                Assert.That(abilityLabel.font.name, Does.StartWith("Maplestory Light"));
                Assert.That(abilityLabel.rectTransform.offsetMin.x, Is.EqualTo(12f).Within(0.01f));
                Assert.That(abilityNameLabel.gameObject.activeSelf, Is.EqualTo(abilityLabel.gameObject.activeSelf));
                if (abilityLabel.gameObject.activeSelf)
                {
                    abilityNameLabel.ForceMeshUpdate();
                    abilityLabel.ForceMeshUpdate();

                    TMP_CharacterInfo nameCharacter = abilityNameLabel.textInfo.characterInfo[0];
                    Assert.That(nameCharacter.fontAsset.name, Does.StartWith("Maplestory Bold"));
                    Assert.That(nameCharacter.color, Is.EqualTo((Color32)unitLabel.color));

                    TMP_CharacterInfo descriptionCharacter = default;
                    bool foundDescriptionGlyph = false;
                    foreach (TMP_CharacterInfo character in abilityLabel.textInfo.characterInfo)
                    {
                        if (!character.isVisible)
                        {
                            continue;
                        }

                        descriptionCharacter = character;
                        foundDescriptionGlyph = true;
                        break;
                    }

                    Assert.That(foundDescriptionGlyph, Is.True);
                    Assert.That(descriptionCharacter.fontAsset.name, Does.StartWith("Maplestory Light"));
                    Color32 expectedDescriptionColor = gradeLabel.color;
                    Assert.That(descriptionCharacter.color, Is.EqualTo(expectedDescriptionColor));
                }
                Image healthStatIcon = shopSlot.transform.Find("HealthStatIcon").GetComponent<Image>();
                Image powerStatIcon = shopSlot.transform.Find("PowerStatIcon").GetComponent<Image>();
                Image rangeStatIcon = shopSlot.transform.Find("RangeStatIcon").GetComponent<Image>();
                Image intervalStatIcon = shopSlot.transform.Find("IntervalStatIcon").GetComponent<Image>();
                TMP_Text healthStatValue = shopSlot.transform.Find("HealthStatValue").GetComponent<TMP_Text>();
                TMP_Text rangeStatValue = shopSlot.transform.Find("RangeStatValue").GetComponent<TMP_Text>();
                TMP_Text intervalStatValue = shopSlot.transform.Find("IntervalStatValue").GetComponent<TMP_Text>();
                Assert.That(healthStatValue.color, Is.EqualTo(gradeLabel.color));
                Assert.That(rangeStatValue.color, Is.EqualTo(gradeLabel.color));
                Assert.That(intervalStatValue.color, Is.EqualTo(gradeLabel.color));
                Assert.That(intervalStatValue.text, Does.EndWith("s"));
                Assert.That(healthStatIcon.sprite.name, Does.StartWith("아이콘_체력"));
                Assert.That(powerStatIcon.sprite.name, Does.StartWith("아이콘_공격력"));
                Assert.That(rangeStatIcon.sprite.name, Does.StartWith("아이콘_사거리"));
                Assert.That(intervalStatIcon.sprite.name, Does.StartWith("아이콘_공격시간"));
                Assert.That(healthStatIcon.rectTransform.anchoredPosition.x, Is.EqualTo(18f).Within(0.01f));
                Assert.That(rangeStatIcon.rectTransform.anchoredPosition.x, Is.EqualTo(74f).Within(0.01f));
                Assert.That(powerStatIcon.rectTransform.anchoredPosition.x, Is.EqualTo(148f).Within(0.01f));
                Assert.That(intervalStatIcon.rectTransform.anchoredPosition.x, Is.EqualTo(132f).Within(0.01f));
                Assert.That(intervalStatIcon.rectTransform.anchoredPosition.y, Is.EqualTo(12f).Within(0.01f));
                Assert.That(shopSlot.transform.Find("Ability"), Is.Not.Null);
                Assert.That(cardRect.rect.size, Is.EqualTo(new Vector2(196f, 148f)));
                Assert.That(cardBackground.color.grayscale, Is.GreaterThan(0.8f));
                Assert.That(cardBackground.sprite, Is.Not.Null);
                Assert.That(cardBackground.sprite.name, Does.StartWith("패널_"));
                Assert.That(cardBackground.sprite.name, Does.Not.Contain("세로"));
                Assert.That(roleIcon.sprite, Is.Not.Null);
                Assert.That(roleIcon.sprite.name, Does.StartWith("역할_"));
                Assert.That(roleIcon.preserveAspect, Is.True);
                Assert.That(unitLabel.color.grayscale, Is.LessThan(0.2f));
                Assert.That(statsLabel.color.grayscale, Is.LessThan(0.35f));

                Assert.That(roleIcon.gameObject.activeSelf, Is.True);
            }

            ShopSlotView hoveredShopSlot = shopSlots[0];
            FieldInfo boundDefinitionField = typeof(ShopSlotView).GetField(
                "boundDefinition",
                BindingFlags.Instance | BindingFlags.NonPublic);
            UnitDefinitionSO hoveredShopDefinition =
                boundDefinitionField.GetValue(hoveredShopSlot) as UnitDefinitionSO;
            hoveredShopSlot.OnPointerEnter(null);
            yield return null;

            TMP_Text shopHoverName = infoPanel.transform.Find("Name").GetComponent<TMP_Text>();
            TMP_Text shopHoverAbilityName = infoPanel.transform.Find(
                "UnitDescriptionPanel/AbilityName").GetComponent<TMP_Text>();
            TMP_Text shopHoverDescription = infoPanel.transform.Find(
                "UnitDescriptionPanel/UnitDescription").GetComponent<TMP_Text>();
            Assert.That(shopHoverName.text, Is.EqualTo(hoveredShopDefinition.DisplayName));
            Assert.That(shopHoverDescription.text, Is.EqualTo(hoveredShopDefinition.Description));
            Assert.That(shopHoverAbilityName.text, Is.Not.Empty);
            Assert.That(infoPanel.IsVisible, Is.True);
            hoveredShopSlot.OnPointerExit(null);
            yield return null;

            Assert.That(boardView.WorldView.ActiveUnitViewCount, Is.EqualTo(1));
            UnitHealthBarView[] initialHealthBars = Object.FindObjectsByType<UnitHealthBarView>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            Assert.That(initialHealthBars, Has.Length.EqualTo(1));
            Assert.That(initialHealthBars[0].HealthRatio, Is.EqualTo(1f));
            Assert.That(initialHealthBars[0].FillRect.anchorMax.x, Is.EqualTo(1f));
            Assert.That(Camera.main.GetComponent("CameraController"), Is.Not.Null);

            RectTransform boardRect = boardView.rectTransform;
            Vector2 placementBoardPosition = boardRect.anchoredPosition;
            Vector2 placementBoardSize = boardRect.sizeDelta;
            RectTransform shopRect = hud.ShopRect;
            Vector2 shopShownPosition = shopRect.anchoredPosition;

            FieldInfo gameField = typeof(GomokuGameController).GetField(
                "game",
                BindingFlags.Instance | BindingFlags.NonPublic);
            GomokuGame game = gameField.GetValue(controller) as GomokuGame;
            BoardUnit enemyUnit = game.Units[0];
            Vector2Int playerPosition = FindOpenAdjacentPosition(game, enemyUnit.X, enemyUnit.Y);

            RectTransform infoPanelRect = infoPanel.transform as RectTransform;
            hud.enabled = false;
            infoPanel.Refresh(enemyUnit, null, controller.PlayerSide);
            TMP_Text infoName = infoPanel.transform.Find("Name").GetComponent<TMP_Text>();
            TMP_Text infoDetails = infoPanel.transform.Find("Details").GetComponent<TMP_Text>();
            TMP_Text healthValue = infoPanel.transform.Find("HealthSlider/ValueText").GetComponent<TMP_Text>();
            Image rarityPanel = infoPanel.GetComponent<Image>();
            Image infoRoleIcon = infoPanel.transform.Find("RoleIcon").GetComponent<Image>();
            TMP_Text unitAbilityName = infoPanel.transform.Find("UnitDescriptionPanel/AbilityName").GetComponent<TMP_Text>();
            TMP_Text unitDescription = infoPanel.transform.Find("UnitDescriptionPanel/UnitDescription").GetComponent<TMP_Text>();
            TMP_Text roleName = infoPanel.transform.Find("RoleAbilityPanel/RoleName").GetComponent<TMP_Text>();
            TMP_Text roleDescription = infoPanel.transform.Find("RoleAbilityPanel/RoleDescription").GetComponent<TMP_Text>();

            Assert.That(infoName.text, Is.EqualTo(enemyUnit.Definition.DisplayName));
            Assert.That(rarityPanel.sprite, Is.Not.Null);
            Assert.That(infoName.text, Does.Not.Contain("■ " + enemyUnit.Definition.RoleDisplayName));
            Assert.That(infoRoleIcon.sprite.name, Does.StartWith("역할_"));
            Assert.That(roleName.text, Does.Contain(enemyUnit.Definition.RoleDisplayName));
            Assert.That(unitAbilityName.text, Is.Not.Empty);
            Assert.That(unitDescription.text, Is.EqualTo(enemyUnit.Definition.Description));
            Assert.That(roleDescription.text, Is.EqualTo(enemyUnit.Definition.RoleDescription));
            Assert.That(infoDetails.text, Does.Contain("<color=#FF0000FF>"));
            Assert.That(infoDetails.text, Does.Contain("<color=#0000FFFF>"));
            Assert.That(infoDetails.text, Does.Not.Contain("적군 ·"));
            Assert.That(infoDetails.text, Does.Not.Contain("쿨다운"));
            Assert.That(healthValue.text, Is.EqualTo($"<b>{enemyUnit.CurrentHealth} / {enemyUnit.Definition.MaxHealth}</b>"));
            Slider healthSlider = infoPanel.transform.Find("HealthSlider").GetComponent<Slider>();
            Slider cooldownSlider = infoPanel.transform.Find("CooldownSlider").GetComponent<Slider>();
            TMP_Text cooldownValue = infoPanel.transform.Find("CooldownSlider/ValueText").GetComponent<TMP_Text>();
            Image healthIcon = infoPanel.transform.Find("HealthSlider/HealthIcon").GetComponent<Image>();
            Image actionIntervalIcon = infoPanel.transform.Find("CooldownSlider/ActionIntervalIcon").GetComponent<Image>();
            Assert.That(cooldownSlider.gameObject.activeSelf, Is.True);
            Assert.That(healthValue.rectTransform.offsetMin.x, Is.EqualTo(32f).Within(0.01f));
            Assert.That(cooldownValue.rectTransform.offsetMin.x, Is.EqualTo(32f).Within(0.01f));
            Assert.That(healthIcon.rectTransform.anchoredPosition.x, Is.EqualTo(17f).Within(0.01f));
            Assert.That(
                actionIntervalIcon.rectTransform.anchoredPosition.x,
                Is.EqualTo(healthIcon.rectTransform.anchoredPosition.x).Within(0.01f));
            Assert.That(actionIntervalIcon.rectTransform.anchoredPosition.y, Is.EqualTo(0f).Within(0.01f));
            Assert.That(healthIcon.sprite, Is.Not.Null);
            Assert.That(actionIntervalIcon.sprite, Is.Not.Null);
            Assert.That(healthSlider.fillRect.GetComponent<Image>().color.g, Is.GreaterThan(healthSlider.fillRect.GetComponent<Image>().color.r));
            Assert.That(cooldownSlider.fillRect.GetComponent<Image>().color.b, Is.GreaterThan(cooldownSlider.fillRect.GetComponent<Image>().color.r));
            Assert.That(cooldownSlider.value, Is.EqualTo(cooldownSlider.maxValue));
            Assert.That(cooldownValue.text, Does.Not.Contain("공격주기"));
            Assert.That(cooldownValue.text, Does.Not.Contain("초"));
            Assert.That(cooldownValue.text, Does.Match(@"^<b>\d+\.\d+s / \d+\.\d+s</b>$"));
            Assert.That(cooldownValue.text, Does.Not.Contain("쿨타임"));
            Assert.That(infoPanel.GetComponent<Image>().color.grayscale, Is.GreaterThan(0.8f));
            Assert.That(infoName.color.grayscale, Is.LessThan(0.2f));
            Assert.That(infoDetails.color.grayscale, Is.LessThan(0.35f));
            Assert.That(infoPanel.transform.Find("RoleColor"), Is.Null);
            Assert.That(infoPanel.Alpha, Is.EqualTo(0f).Within(0.01f));
            Assert.That(
                infoPanelRect.anchoredPosition.x,
                Is.EqualTo(infoPanel.ShownPosition.x + infoPanel.ShowOffset).Within(0.1f));

            yield return new WaitForSecondsRealtime(infoPanel.ShowDuration * 0.5f);
            Assert.That(infoPanel.Alpha, Is.GreaterThan(0f).And.LessThan(1f));
            Assert.That(infoPanelRect.anchoredPosition.x, Is.GreaterThan(infoPanel.ShownPosition.x));

            yield return new WaitForSecondsRealtime(infoPanel.ShowDuration * 0.6f);
            yield return new WaitForEndOfFrame();
            Assert.That(infoPanel.Alpha, Is.EqualTo(1f).Within(0.01f));
            Assert.That(
                infoPanelRect.anchoredPosition.x,
                Is.EqualTo(infoPanel.ShownPosition.x).Within(0.1f));

            infoPanel.Hide();
            yield return new WaitForSecondsRealtime(infoPanel.HideDuration * 0.5f);
            Assert.That(infoPanel.Alpha, Is.GreaterThan(0f).And.LessThan(1f));
            Assert.That(infoPanelRect.anchoredPosition.x, Is.GreaterThan(infoPanel.ShownPosition.x));

            yield return new WaitForSecondsRealtime(infoPanel.HideDuration * 0.6f);
            yield return new WaitForEndOfFrame();
            Assert.That(infoPanel.Alpha, Is.EqualTo(0f).Within(0.01f));
            Assert.That(
                infoPanelRect.anchoredPosition.x,
                Is.EqualTo(infoPanel.ShownPosition.x + infoPanel.HideOffset).Within(0.1f));
            hud.enabled = true;

            InvokePrivate(controller, "HandleShopSelection", 0);
            ShopSlotView selectedSlot = null;
            foreach (ShopSlotView shopSlot in shopSlots)
            {
                if (shopSlot.IsSelected)
                {
                    selectedSlot = shopSlot;
                    break;
                }
            }

            Assert.That(selectedSlot, Is.Not.Null);
            placementCursor.UpdatePointerPresentation(
                new Vector2(Screen.width * 0.5f, Screen.height * 0.95f));
            Assert.That(placementCursor.IsCursorVisible, Is.True);
            InvokePrivate(controller, "HandleBoardClick", playerPosition.x, playerPosition.y);

            Assert.That(phaseText.text, Is.EqualTo("전투"));
            Assert.That(selectedSlot.IsSelected, Is.False);
            Assert.That(placementCursor.IsCursorVisible, Is.False);
            Assert.That(boardView.PointerState.Mode, Is.EqualTo(BoardPointerMode.None));
            Assert.That(boardView.WorldView.ActiveUnitViewCount, Is.EqualTo(2));
            UnitHealthBarView[] combatHealthBars = Object.FindObjectsByType<UnitHealthBarView>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            Assert.That(combatHealthBars, Has.Length.EqualTo(2));
            Assert.That(combatSlider.gameObject.activeSelf, Is.True);
            Assert.That(combatSlider.value, Is.EqualTo(0f));
            Assert.That(speedButtonRoot.gameObject.activeSelf, Is.True);
            Assert.That(speedText.text, Is.EqualTo("x2"));
            Assert.That(Time.timeScale, Is.EqualTo(2f));
            Assert.That(shopRect.gameObject.activeSelf, Is.True);

            yield return new WaitForSecondsRealtime(
                controller.ShopHideDelayAfterPlacement * 0.5f);
            yield return new WaitForEndOfFrame();
            Assert.That(combatSlider.value, Is.EqualTo(0f));
            Assert.That(shopRect.gameObject.activeSelf, Is.True);
            Assert.That(shopRect.anchoredPosition.x, Is.EqualTo(shopShownPosition.x).Within(0.1f));
            Assert.That(shopRect.anchoredPosition.y, Is.EqualTo(shopShownPosition.y).Within(0.1f));
            Assert.That(boardRect.anchoredPosition.x, Is.EqualTo(placementBoardPosition.x).Within(0.1f));
            Assert.That(boardRect.anchoredPosition.y, Is.EqualTo(placementBoardPosition.y).Within(0.1f));
            Assert.That(boardRect.sizeDelta.x, Is.EqualTo(placementBoardSize.x).Within(0.1f));
            Assert.That(boardRect.sizeDelta.y, Is.EqualTo(placementBoardSize.y).Within(0.1f));

            yield return new WaitForSecondsRealtime(
                controller.ShopHideDelayAfterPlacement * 0.5f
                + hud.ShopHideDuration * 0.7f);
            yield return new WaitForEndOfFrame();
            Assert.That(combatSlider.value, Is.EqualTo(0f));
            Assert.That(shopRect.gameObject.activeSelf, Is.True);
            Assert.That(shopRect.anchoredPosition.y, Is.LessThan(shopShownPosition.y));
            Assert.That(boardRect.anchoredPosition.x, Is.EqualTo(placementBoardPosition.x).Within(0.1f));
            Assert.That(boardRect.anchoredPosition.y, Is.EqualTo(placementBoardPosition.y).Within(0.1f));
            Assert.That(boardRect.sizeDelta.x, Is.EqualTo(placementBoardSize.x).Within(0.1f));
            Assert.That(boardRect.sizeDelta.y, Is.EqualTo(placementBoardSize.y).Within(0.1f));
            AssertUnitPresentationAlignment(boardView, combatHealthBars);

            float shopTransitionTimeout = 1f;
            while (shopRect.gameObject.activeSelf && shopTransitionTimeout > 0f)
            {
                shopTransitionTimeout -= Time.deltaTime;
                yield return null;
            }

            Assert.That(shopTransitionTimeout, Is.GreaterThan(0f));
            Assert.That(combatSlider.value, Is.EqualTo(0f));
            yield return new WaitForSecondsRealtime(hud.ShopHideDuration * 0.5f);
            yield return new WaitForEndOfFrame();
            Assert.That(boardRect.sizeDelta.x, Is.GreaterThan(placementBoardSize.x));
            Assert.That(combatSlider.value, Is.EqualTo(0f));
            AssertUnitPresentationAlignment(boardView, combatHealthBars);

            while (combatSlider.value <= 0f && shopTransitionTimeout > 0f)
            {
                shopTransitionTimeout -= Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.That(shopTransitionTimeout, Is.GreaterThan(0f));
            Assert.That(shopRect.gameObject.activeSelf, Is.False);
            Bounds boardBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(
                hud.transform,
                boardRect);
            Bounds turnBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(
                hud.transform,
                turnStatusView.transform);
            Assert.That(boardBounds.max.y, Is.LessThan(turnBounds.min.y));

            Assert.That(combatSlider.value, Is.GreaterThan(0f));
            Assert.That(SoundManager.Instance.CurrentMusic, Is.EqualTo(controller.BattleMusic));
            Assert.That(SoundManager.Instance.IsMusicPlaying, Is.True);
            Assert.That(SoundManager.Instance.IsMusicLooping, Is.True);
            Assert.That(SoundManager.Instance.IsMusicCrossfading, Is.True);
            Assert.That(CountPlayingMusicSources(), Is.EqualTo(2));

            float timeout = 12f;
            while (combatSlider.value < 0.999f && timeout > 0f)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }

            Assert.That(timeout, Is.GreaterThan(0f), "Combat did not finish within its configured duration.");
            Assert.That(phaseText.text, Is.EqualTo("전투"));
            Assert.That(combatSlider.gameObject.activeSelf, Is.True);
            Assert.That(shopRect.gameObject.activeSelf, Is.False);
            Vector2 combatBoardSize = boardRect.sizeDelta;

            yield return new WaitForSecondsRealtime(controller.CombatEndDelay * 0.5f);
            yield return new WaitForEndOfFrame();
            Assert.That(phaseText.text, Is.EqualTo("전투"));
            Assert.That(combatSlider.gameObject.activeSelf, Is.True);
            Assert.That(shopRect.gameObject.activeSelf, Is.False);
            Assert.That(boardRect.sizeDelta.x, Is.EqualTo(combatBoardSize.x).Within(0.1f));
            Assert.That(boardRect.sizeDelta.y, Is.EqualTo(combatBoardSize.y).Within(0.1f));

            while (phaseText.text == "전투" && timeout > 0f)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }

            Assert.That(timeout, Is.GreaterThan(0f), "Combat did not finish within its configured duration.");
            Assert.That(turnText.text, Is.EqualTo("2턴"));
            Assert.That(phaseText.text, Is.EqualTo("적 턴"));
            Assert.That(combatSlider.gameObject.activeSelf, Is.False);
            Assert.That(speedButtonRoot.gameObject.activeSelf, Is.True);
            Assert.That(speedText.text, Is.EqualTo("x2"));
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(shopRect.gameObject.activeSelf, Is.False);
            Assert.That(SoundManager.Instance.CurrentMusic, Is.EqualTo(controller.PrepareMusic));
            Assert.That(SoundManager.Instance.IsMusicPlaying, Is.True);
            Assert.That(SoundManager.Instance.IsMusicLooping, Is.True);
            Assert.That(SoundManager.Instance.IsMusicCrossfading, Is.True);
            Assert.That(CountPlayingMusicSources(), Is.EqualTo(2));

            yield return new WaitForSecondsRealtime(hud.ShopShowDuration * 0.5f);
            yield return new WaitForEndOfFrame();
            Assert.That(shopRect.gameObject.activeSelf, Is.False);
            Assert.That(boardRect.sizeDelta.x, Is.LessThan(combatBoardSize.x));
            Assert.That(boardRect.sizeDelta.x, Is.GreaterThan(placementBoardSize.x));

            float shopShowTimeout = 1f;
            while (!shopRect.gameObject.activeSelf && shopShowTimeout > 0f)
            {
                shopShowTimeout -= Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.That(shopShowTimeout, Is.GreaterThan(0f));
            Assert.That(shopRect.gameObject.activeSelf, Is.True);
            Assert.That(boardRect.anchoredPosition.x, Is.EqualTo(placementBoardPosition.x).Within(0.1f));
            Assert.That(boardRect.anchoredPosition.y, Is.EqualTo(placementBoardPosition.y).Within(0.1f));
            Assert.That(boardRect.sizeDelta.x, Is.EqualTo(placementBoardSize.x).Within(0.1f));
            Assert.That(boardRect.sizeDelta.y, Is.EqualTo(placementBoardSize.y).Within(0.1f));

            yield return new WaitForSecondsRealtime(hud.ShopShowDuration + 0.05f);
            Assert.That(shopRect.anchoredPosition.x, Is.EqualTo(shopShownPosition.x).Within(0.1f));
            Assert.That(shopRect.anchoredPosition.y, Is.EqualTo(shopShownPosition.y).Within(0.1f));

            Object.Destroy(boardView.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ResultPanelUsesPopupTween()
        {
            AsyncOperation load = SceneManager.LoadSceneAsync("GomokuMvp", LoadSceneMode.Single);
            while (!load.isDone)
            {
                yield return null;
            }

            yield return null;
            yield return null;

            GomokuHud hud = Object.FindFirstObjectByType<GomokuHud>();
            RectTransform panel = hud.transform.Find("ResultPanel") as RectTransform;
            CanvasGroup group = panel.GetComponent<CanvasGroup>();
            TMP_Text title = panel.Find("Title").GetComponent<TMP_Text>();
            TMP_Text score = panel.Find("Score").GetComponent<TMP_Text>();
            TMP_Text buttonText = panel.Find("ContinueButton/Text").GetComponent<TMP_Text>();

            hud.HideResult();
            Vector3 shownScale = panel.localScale;
            hud.ShowResult("승리", "3 : 0", "계속");

            Transform backdrop = hud.transform.Find("ResultBackdrop");
            Assert.That(backdrop, Is.Not.Null);
            Image backdropImage = backdrop.GetComponent<Image>();
            CanvasGroup backdropGroup = backdrop.GetComponent<CanvasGroup>();
            RectTransform backdropRect = (RectTransform)backdrop;

            Assert.That(panel.gameObject.activeSelf, Is.True);
            Assert.That(backdrop.gameObject.activeSelf, Is.True);
            Assert.That(backdrop.GetSiblingIndex(), Is.EqualTo(panel.GetSiblingIndex() - 1));
            Assert.That(backdropRect.anchorMin, Is.EqualTo(Vector2.zero));
            Assert.That(backdropRect.anchorMax, Is.EqualTo(Vector2.one));
            Assert.That(
                backdropImage.color,
                Is.EqualTo(new Color(0f, 0f, 0f, hud.ResultBackdropOpacity)));
            Assert.That(backdropGroup.alpha, Is.EqualTo(0f).Within(0.01f));
            Assert.That(backdropGroup.blocksRaycasts, Is.True);
            Assert.That(group.alpha, Is.EqualTo(0f).Within(0.01f));
            Assert.That(panel.localScale.x, Is.EqualTo(shownScale.x * hud.ResultStartScale).Within(0.01f));
            Assert.That(group.interactable, Is.False);
            Assert.That(title.text, Is.EqualTo("승리"));
            Assert.That(score.text, Is.EqualTo("3 : 0"));
            Assert.That(buttonText.text, Is.EqualTo("계속"));

            yield return new WaitForSecondsRealtime(hud.ResultShowDuration * 0.5f);
            Assert.That(backdropGroup.alpha, Is.GreaterThan(0f));
            Assert.That(group.alpha, Is.GreaterThan(0f));
            Assert.That(panel.localScale.x, Is.GreaterThan(shownScale.x * hud.ResultStartScale));
            Assert.That(group.interactable, Is.False);

            yield return new WaitForSecondsRealtime(hud.ResultShowDuration * 0.6f + 0.05f);
            Assert.That(backdropGroup.alpha, Is.EqualTo(1f).Within(0.01f));
            Assert.That(group.alpha, Is.EqualTo(1f).Within(0.01f));
            Assert.That(panel.localScale.x, Is.EqualTo(shownScale.x).Within(0.01f));
            Assert.That(group.interactable, Is.True);

            hud.HideResult();
            Assert.That(panel.gameObject.activeSelf, Is.False);
            Assert.That(backdrop.gameObject.activeSelf, Is.False);
            Assert.That(backdropGroup.blocksRaycasts, Is.False);
        }

        [UnityTest]
        public IEnumerator TitleSceneButtonUsesSharedClickFeedback()
        {
            AsyncOperation load = SceneManager.LoadSceneAsync("Title", LoadSceneMode.Single);
            while (!load.isDone)
            {
                yield return null;
            }

            yield return null;
            yield return null;

            UiButtonSfxFeedback feedback = Object.FindFirstObjectByType<UiButtonSfxFeedback>(
                FindObjectsInactive.Include);
            AnimatedTitleScreenController titleController =
                Object.FindFirstObjectByType<AnimatedTitleScreenController>(
                    FindObjectsInactive.Include);
            Assert.That(feedback, Is.Not.Null);
            Assert.That(titleController, Is.Not.Null);
            Assert.That(titleController.TitleMusic.name, Is.EqualTo("battle"));
            Assert.That(titleController.MusicFadeDuration, Is.GreaterThan(0f));
            Assert.That(
                SoundManager.Instance.CurrentMusic,
                Is.EqualTo(titleController.TitleMusic));
            Assert.That(SoundManager.Instance.IsMusicPlaying, Is.True);
            Assert.That(SoundManager.Instance.IsMusicLooping, Is.True);
            feedback.BindButtons();
            Assert.That(feedback.BoundButtonCount, Is.EqualTo(2));
            Assert.That(feedback.ClickSfx.name, Is.EqualTo("drop_002"));
            Assert.That(feedback.PitchRange, Is.EqualTo(new Vector2(0.92f, 1.08f)));

            SoundManager.Instance.StopAllSfx();
            feedback.PlayFeedback();
            AudioSource playedSource = FindConfiguredSfxSource();
            Assert.That(playedSource.clip.name, Is.EqualTo("drop_002"));
            Assert.That(playedSource.pitch, Is.InRange(0.92f, 1.08f));

            yield return new WaitForSecondsRealtime(titleController.MusicFadeDuration + 0.05f);
            Assert.That(SoundManager.Instance.IsMusicCrossfading, Is.False);
            Assert.That(CountPlayingMusicSources(), Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator SceneTransitionFadeDoesNotSkipAfterLongFrame()
        {
            AsyncOperation load = SceneManager.LoadSceneAsync("Title", LoadSceneMode.Single);
            while (!load.isDone)
            {
                yield return null;
            }

            SceneTransitionController transition =
                Object.FindFirstObjectByType<SceneTransitionController>(
                    FindObjectsInactive.Include);
            Assert.That(transition, Is.Not.Null);

            while (transition.IsTransitioning)
            {
                yield return null;
            }

            System.Threading.Thread.Sleep(800);
            yield return null;
            Assert.That(Time.unscaledDeltaTime, Is.GreaterThan(0.4f));

            SceneTransitionController.LoadMainGame();
            Assert.That(transition.Alpha, Is.LessThan(1f));

            while (SceneManager.GetActiveScene().name != SceneTransitionController.MainGameSceneName)
            {
                yield return null;
            }

            Assert.That(transition.Alpha, Is.EqualTo(1f).Within(0.01f));

            yield return new WaitForSecondsRealtime(0.1f);
            Assert.That(transition.Alpha, Is.GreaterThan(0f).And.LessThan(1f));

            while (transition.IsTransitioning)
            {
                yield return null;
            }

            Assert.That(transition.Alpha, Is.EqualTo(0f).Within(0.01f));
        }

        [UnityTest]
        public IEnumerator UnitActionAudioUsesRolePitchAndSpecialClips()
        {
            AsyncOperation load = SceneManager.LoadSceneAsync("GomokuMvp", LoadSceneMode.Single);
            while (!load.isDone)
            {
                yield return null;
            }

            yield return null;

            GomokuGameController controller = Object.FindFirstObjectByType<GomokuGameController>();
            Assert.That(controller, Is.Not.Null);
            controller.StopAllCoroutines();
            controller.enabled = false;

            FieldInfo catalogField = typeof(GomokuGameController).GetField(
                "unitCatalog",
                BindingFlags.Instance | BindingFlags.NonPublic);
            UnitCatalogSO catalog = catalogField.GetValue(controller) as UnitCatalogSO;
            Assert.That(catalog, Is.Not.Null);

            UnitView[] prefabs = catalog.Units
                .Select(definition => definition.Presentation.WorldPrefab)
                .Distinct()
                .ToArray();
            Assert.That(prefabs, Has.Length.EqualTo(4));
            foreach (UnitView prefab in prefabs)
            {
                Assert.That(prefab.AttackPopSfx.Count, Is.EqualTo(4));
                Assert.That(
                    prefab.AttackPopSfx.Select(clip => clip != null ? clip.name : string.Empty),
                    Is.EquivalentTo(new[] { "pop_1", "pop_2", "pop_3", "pop_4" }));
                Assert.That(prefab.ArrowAttackSfx.name, Is.EqualTo("djartmusic-real-swish_3-304242"));
                Assert.That(prefab.HealingMagicSfx.name, Is.EqualTo("yodguard-healing-magic-1-378665"));
                Assert.That(prefab.NinjaAttackSfx.name, Is.EqualTo("dragon-studio-bell-ring-390294"));
                Assert.That(prefab.CasterMagicSfx.name, Is.EqualTo("universfield-spell-casting-229208"));
            }

            var cases = new[]
            {
                new { UnitId = "common-guardian", Kind = UnitActionKind.Damage, Expected = "pop_", MinPitch = 0.72f, MaxPitch = 0.86f },
                new { UnitId = "common-vanguard", Kind = UnitActionKind.Damage, Expected = "pop_", MinPitch = 0.88f, MaxPitch = 1.02f },
                new { UnitId = "epic-shaman", Kind = UnitActionKind.Damage, Expected = "pop_", MinPitch = 1.05f, MaxPitch = 1.18f },
                new { UnitId = "epic-sniper", Kind = UnitActionKind.Damage, Expected = "pop_", MinPitch = 1.08f, MaxPitch = 1.22f },
                new { UnitId = "epic-mage", Kind = UnitActionKind.Damage, Expected = "universfield-spell-casting-229208", MinPitch = 0.94f, MaxPitch = 1.06f },
                new { UnitId = "legendary-storm-sage", Kind = UnitActionKind.Damage, Expected = "universfield-spell-casting-229208", MinPitch = 1.08f, MaxPitch = 1.22f },
                new { UnitId = "common-marksman", Kind = UnitActionKind.Damage, Expected = "djartmusic-real-swish_3-304242", MinPitch = 0.96f, MaxPitch = 1.08f },
                new { UnitId = "common-healer", Kind = UnitActionKind.Heal, Expected = "yodguard-healing-magic-1-378665", MinPitch = 0.96f, MaxPitch = 1.06f },
                new { UnitId = "rare-ninja", Kind = UnitActionKind.Damage, Expected = "dragon-studio-bell-ring-390294", MinPitch = 0.98f, MaxPitch = 1.12f }
            };

            int placementOrder = 1000;
            foreach (var testCase in cases)
            {
                UnitDefinitionSO definition = catalog.Units.Single(
                    candidate => candidate.UnitId == testCase.UnitId);
                UnitView actor = Object.Instantiate(definition.Presentation.WorldPrefab);
                actor.Bind(
                    new BoardUnit(
                        definition,
                        StoneColor.White,
                        0,
                        0,
                        placementOrder++),
                    definition.Presentation);

                SoundManager.Instance.StopAllSfx();
                InvokePrivate(actor, "PlayActionSfx", testCase.Kind);

                AudioSource playedSource = SoundManager.Instance
                    .GetComponentsInChildren<AudioSource>()
                    .FirstOrDefault(source =>
                        source.clip != null
                        && source.gameObject.name.StartsWith("SFX"));
                Assert.That(playedSource, Is.Not.Null, testCase.UnitId);
                if (testCase.Expected == "pop_")
                {
                    Assert.That(playedSource.clip.name, Does.StartWith(testCase.Expected));
                }
                else
                {
                    Assert.That(playedSource.clip.name, Is.EqualTo(testCase.Expected));
                }

                Assert.That(
                    playedSource.pitch,
                    Is.InRange(testCase.MinPitch, testCase.MaxPitch),
                    testCase.UnitId);

                Object.Destroy(actor.gameObject);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator ProjectileCombatResultsWaitForArrival()
        {
            float originalTimeScale = Time.timeScale;
            Time.timeScale = 1f;
            UnitCatalogSO catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<UnitCatalogSO>(
                "Assets/06. Data/00. Units/UnitCatalog.asset");
            Assert.That(catalog, Is.Not.Null);
            UnitDefinitionSO actorDefinition = catalog.Units.Single(
                definition => definition.UnitId == "common-marksman");
            UnitDefinitionSO targetDefinition = catalog.Units.Single(
                definition => definition.UnitId == "common-vanguard");

            var root = new GameObject("ProjectileCombatTimingTest");
            BoardWorldView worldView = root.AddComponent<BoardWorldView>();
            UnitView actorView = Object.Instantiate(
                actorDefinition.Presentation.WorldPrefab,
                root.transform);
            UnitView targetView = Object.Instantiate(
                targetDefinition.Presentation.WorldPrefab,
                root.transform);
            actorView.transform.localPosition = Vector3.zero;
            targetView.transform.localPosition = Vector3.right;

            var actorUnit = new BoardUnit(
                actorDefinition,
                StoneColor.White,
                0,
                0,
                2000);
            var targetUnit = new BoardUnit(
                targetDefinition,
                StoneColor.Black,
                1,
                0,
                2001);
            actorView.Bind(actorUnit, actorDefinition.Presentation);
            targetView.Bind(targetUnit, targetDefinition.Presentation);
            Assert.That(actorView.UsesProjectile, Is.True);

            FieldInfo unitViewsField = typeof(BoardWorldView).GetField(
                "unitViews",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var unitViews = unitViewsField.GetValue(worldView)
                as Dictionary<BoardUnit, UnitView>;
            unitViews.Add(actorUnit, actorView);
            unitViews.Add(targetUnit, targetView);

            bool resultsPresented = false;
            var actionEvent = new CombatActionEvent(
                actorUnit,
                UnitActionKind.Damage,
                new[]
                {
                    new CombatEffectResult(
                        targetUnit,
                        CombatEffectKind.Damage,
                        1,
                        false)
                });

            worldView.PlayCombatAction(actionEvent, () => resultsPresented = true);
            Assert.That(resultsPresented, Is.False);

            yield return new WaitForSeconds(0.3f);
            Assert.That(resultsPresented, Is.False);

            yield return new WaitForSeconds(0.3f);
            Assert.That(resultsPresented, Is.True);

            Object.Destroy(root);
            SoundManager.Instance.StopAllSfx();
            Time.timeScale = originalTimeScale;
            yield return null;
        }

        [UnityTest]
        public IEnumerator SniperProjectileTravelsStraightThroughFarthestTarget()
        {
            float originalTimeScale = Time.timeScale;
            Time.timeScale = 1f;
            UnitCatalogSO catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<UnitCatalogSO>(
                "Assets/06. Data/00. Units/UnitCatalog.asset");
            Assert.That(catalog, Is.Not.Null);
            UnitDefinitionSO actorDefinition = catalog.Units.Single(
                definition => definition.UnitId == "epic-sniper");
            UnitDefinitionSO targetDefinition = catalog.Units.Single(
                definition => definition.UnitId == "common-vanguard");

            var root = new GameObject("PiercingProjectileTest");
            BoardWorldView worldView = root.AddComponent<BoardWorldView>();
            UnitView actorView = Object.Instantiate(
                actorDefinition.Presentation.WorldPrefab,
                root.transform);
            actorView.transform.localPosition = Vector3.zero;
            var actorUnit = new BoardUnit(
                actorDefinition,
                StoneColor.White,
                0,
                0,
                3000);
            actorView.Bind(actorUnit, actorDefinition.Presentation);

            var targetUnits = new BoardUnit[2];
            var targetViews = new UnitView[2];
            for (int index = 0; index < targetUnits.Length; index++)
            {
                targetViews[index] = Object.Instantiate(
                    targetDefinition.Presentation.WorldPrefab,
                    root.transform);
                targetViews[index].transform.localPosition = Vector3.right * (index + 1);
                targetUnits[index] = new BoardUnit(
                    targetDefinition,
                    StoneColor.Black,
                    index + 1,
                    0,
                    3001 + index);
                targetViews[index].Bind(targetUnits[index], targetDefinition.Presentation);
            }

            FieldInfo unitViewsField = typeof(BoardWorldView).GetField(
                "unitViews",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var unitViews = unitViewsField.GetValue(worldView)
                as Dictionary<BoardUnit, UnitView>;
            unitViews.Add(actorUnit, actorView);
            for (int index = 0; index < targetUnits.Length; index++)
            {
                unitViews.Add(targetUnits[index], targetViews[index]);
            }

            bool resultsPresented = false;
            var actionEvent = new CombatActionEvent(
                actorUnit,
                UnitActionKind.Damage,
                targetUnits.Select(target => new CombatEffectResult(
                    target,
                    CombatEffectKind.Damage,
                    1,
                    false)).ToArray());

            worldView.PlayCombatAction(actionEvent, () => resultsPresented = true);
            yield return new WaitForSeconds(0.3f);

            ProjectileVfxView projectile = root.GetComponentInChildren<ProjectileVfxView>();
            Assert.That(projectile, Is.Not.Null);
            Assert.That(projectile.UsesLinearTrajectory, Is.True);
            Assert.That(projectile.GetComponent<TrailRenderer>(), Is.Not.Null);
            Assert.That(Mathf.Abs(projectile.DestinationLocalPosition.y), Is.LessThan(0.01f));
            Assert.That(projectile.DestinationLocalPosition.x, Is.GreaterThan(2f));
            Assert.That(resultsPresented, Is.False);

            yield return new WaitForSeconds(0.3f);
            Assert.That(resultsPresented, Is.True);

            Object.Destroy(root);
            SoundManager.Instance.StopAllSfx();
            Time.timeScale = originalTimeScale;
            yield return null;
        }

        [UnityTest]
        public IEnumerator UnitViewDotweenAnimationsCompleteAndRestoreState()
        {
            UnitView actor = UnitView.CreateRuntimePlaceholder(null);
            UnitView target = UnitView.CreateRuntimePlaceholder(null);
            actor.transform.localPosition = Vector3.zero;
            target.transform.localPosition = Vector3.right;

            actor.PlayAction(target);
            yield return new WaitForSeconds(0.24f);

            Assert.That(Vector3.Distance(actor.transform.localPosition, Vector3.zero), Is.LessThan(0.001f));

            bool deathCompleted = false;
            actor.PlayDeath(0.1f, () => deathCompleted = true);
            yield return new WaitForSeconds(0.12f);

            Assert.That(deathCompleted, Is.True);
            Assert.That(Vector3.Distance(actor.transform.localScale, Vector3.zero), Is.LessThan(0.001f));

            Object.Destroy(actor.gameObject);
            Object.Destroy(target.gameObject);
            yield return null;
        }

        private static Vector2Int FindOpenAdjacentPosition(GomokuGame game, int centerX, int centerY)
        {
            for (int x = Mathf.Max(0, centerX - 1); x <= Mathf.Min(GomokuGame.BoardSize - 1, centerX + 1); x++)
            {
                for (int y = Mathf.Max(0, centerY - 1); y <= Mathf.Min(GomokuGame.BoardSize - 1, centerY + 1); y++)
                {
                    if (game.GetUnit(x, y) == null)
                    {
                        return new Vector2Int(x, y);
                    }
                }
            }

            Assert.Fail("No open adjacent board position was found.");
            return default;
        }

        private static void AssertUnitPresentationAlignment(
            GomokuBoardView boardView,
            IEnumerable<UnitHealthBarView> healthBars)
        {
            RectTransform boardRect = boardView.rectTransform;
            Rect rect = boardRect.rect;
            float boardSize = Mathf.Min(rect.width, rect.height);
            float margin = boardSize * 0.045f;
            var gridRect = new Rect(
                -boardSize * 0.5f + margin,
                -boardSize * 0.5f + margin,
                boardSize - margin * 2f,
                boardSize - margin * 2f);
            float spacing = gridRect.width / (GomokuGame.BoardSize - 1);

            foreach (UnitHealthBarView healthBar in healthBars)
            {
                BoardUnit unit = healthBar.Unit;
                var cellPosition = new Vector2(
                    gridRect.xMin + unit.X * spacing,
                    gridRect.yMin + unit.Y * spacing);
                Vector2 expectedUnitScreenPosition = RectTransformUtility.WorldToScreenPoint(
                    null,
                    boardRect.TransformPoint(cellPosition));
                Vector2 actualUnitScreenPosition = Camera.main.WorldToScreenPoint(
                    boardView.WorldView.CellToWorld(unit.X, unit.Y));
                Assert.That(
                    Vector2.Distance(actualUnitScreenPosition, expectedUnitScreenPosition),
                    Is.LessThan(1.5f),
                    $"World unit at ({unit.X}, {unit.Y}) drifted from its board cell.");

                Vector2 expectedHealthScreenPosition = RectTransformUtility.WorldToScreenPoint(
                    null,
                    boardRect.TransformPoint(
                        cellPosition + Vector2.down * spacing * 0.48f));
                Vector2 actualHealthScreenPosition = RectTransformUtility.WorldToScreenPoint(
                    null,
                    healthBar.transform.position);
                Assert.That(
                    Vector2.Distance(actualHealthScreenPosition, expectedHealthScreenPosition),
                    Is.LessThan(1.5f),
                    $"Health UI at ({unit.X}, {unit.Y}) drifted from its unit.");
            }
        }

        private static int CountPlayingMusicSources()
        {
            return SoundManager.Instance
                .GetComponentsInChildren<AudioSource>()
                .Count(source =>
                    source.isPlaying
                    && source.gameObject.name.StartsWith("Music"));
        }

        private static AudioSource FindConfiguredSfxSource()
        {
            AudioSource source = SoundManager.Instance
                .GetComponentsInChildren<AudioSource>()
                .FirstOrDefault(candidate =>
                    candidate.clip != null
                    && candidate.gameObject.name.StartsWith("SFX"));
            Assert.That(source, Is.Not.Null);
            return source;
        }

        private static void InvokePrivate(object target, string methodName, params object[] arguments)
        {
            MethodInfo method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(target, arguments);
        }

        private static Transform FindDirectChild(Transform parent, string childName)
        {
            foreach (Transform child in parent)
            {
                if (child.name == childName)
                {
                    return child;
                }
            }

            return null;
        }
    }
}
