﻿using UnityEngine;

namespace QSB.Utility
{
    public static class GOExtensions
    {
        public static void Show(this GameObject gameObject) => SetVisibility(gameObject, true);

        public static void Hide(this GameObject gameObject) => SetVisibility(gameObject, false);

        private static void SetVisibility(GameObject gameObject, bool isVisible)
        {
            var renderers = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (var renderer in renderers)
            {
                renderer.enabled = isVisible;
            }
        }
    }
}
