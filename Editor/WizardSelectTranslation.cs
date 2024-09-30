#if UNITY_EDITOR
using UnityEditor;

namespace Myy
{
    public class WizardSelectTranslation : ScriptableWizard
    {
        public TranslationStrings.HumanReadableLang lang;

        [MenuItem("Voyage / World Lock Setup - Settings", priority = 1)]
        static void CreateWizard()
        {
            ScriptableWizard.DisplayWizard<WizardSelectTranslation>(
                "World Lock Setup - Settings",
                "Set");
        }

        void OnWizardCreate()
        {
            TranslationStrings.SetLang(lang);
        }
    }
}

#endif