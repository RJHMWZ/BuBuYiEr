using UnityEngine;

namespace UIAnimationPresets
{
    [CreateAssetMenu(menuName = "UI Animation/Animation Preset", fileName = "UIAnimationPreset")]
    public sealed class UIAnimationPresetAsset : ScriptableObject
    {
        public UIAnimationDefinition animation = new UIAnimationDefinition();
    }
}
