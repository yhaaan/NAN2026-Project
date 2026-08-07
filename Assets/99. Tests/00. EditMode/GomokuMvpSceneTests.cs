using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NAN2026.Gomoku.Tests
{
    public sealed class GomokuMvpSceneTests
    {
        private const string ScenePath = "Assets/00. Scenes/GomokuMvp.unity";

        [Test]
        public void SceneContainsCanvasShopPrefabsAndUnitCatalog()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            Canvas canvas = FindInScene<Canvas>(scene);
            GomokuHud hud = FindInScene<GomokuHud>(scene);
            GomokuGameController controller = FindInScene<GomokuGameController>(scene);
            GomokuBoardView boardView = FindInScene<GomokuBoardView>(scene);
            ShopSlotView[] shopSlots = FindAllInScene<ShopSlotView>(scene);

            Assert.That(canvas, Is.Not.Null);
            Assert.That(hud, Is.Not.Null);
            Assert.That(controller, Is.Not.Null);
            Assert.That(boardView, Is.Not.Null);
            Assert.That(boardView.GetComponent<CanvasRenderer>(), Is.Not.Null);
            Assert.That(shopSlots, Has.Length.EqualTo(ShopState.SlotCount));

            foreach (ShopSlotView shopSlot in shopSlots)
            {
                GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(shopSlot.gameObject);
                Assert.That(source, Is.Not.Null);
                Assert.That(AssetDatabase.GetAssetPath(source), Is.EqualTo("Assets/02. Prefabs/02. UI/ShopSlot.prefab"));
            }

            Transform resultPanel = FindTransform(scene, "ResultPanel");
            Assert.That(resultPanel, Is.Not.Null);
            GameObject resultSource = PrefabUtility.GetCorrespondingObjectFromSource(resultPanel.gameObject);
            Assert.That(AssetDatabase.GetAssetPath(resultSource), Is.EqualTo("Assets/02. Prefabs/02. UI/ResultPanel.prefab"));

            var serializedController = new SerializedObject(controller);
            var catalog = serializedController.FindProperty("unitCatalog").objectReferenceValue as UnitCatalogSO;
            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.Units, Has.Count.EqualTo(4));

            var serializedHud = new SerializedObject(hud);
            Assert.That(serializedHud.FindProperty("shopSlots").arraySize, Is.EqualTo(ShopState.SlotCount));

            var serializedBoard = new SerializedObject(boardView);
            Assert.That(serializedBoard.FindProperty("attackDamagePopup").objectReferenceValue, Is.Not.Null);
            Assert.That(serializedBoard.FindProperty("hitDamagePopup").objectReferenceValue, Is.Not.Null);
            Assert.That(serializedBoard.FindProperty("healPopup").objectReferenceValue, Is.Not.Null);

            EditorBuildSettingsScene[] buildScenes = EditorBuildSettings.scenes;
            Assert.That(buildScenes, Has.Length.GreaterThanOrEqualTo(1));
            Assert.That(buildScenes[0].enabled, Is.True);
            Assert.That(buildScenes[0].path, Is.EqualTo(ScenePath));
        }

        private static T FindInScene<T>(Scene scene) where T : Component
        {
            T[] components = FindAllInScene<T>(scene);
            return components.Length > 0 ? components[0] : null;
        }

        private static T[] FindAllInScene<T>(Scene scene) where T : Component
        {
            var results = new System.Collections.Generic.List<T>();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                results.AddRange(root.GetComponentsInChildren<T>(true));
            }

            return results.ToArray();
        }

        private static Transform FindTransform(Scene scene, string objectName)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
                foreach (Transform candidate in transforms)
                {
                    if (candidate.name == objectName)
                    {
                        return candidate;
                    }
                }
            }

            return null;
        }
    }
}
