using System.Linq;
using UnityEngine;

namespace RealFuels
{
    public class ModulePatchableEngineConfigs : ModuleEngineConfigs
    {
        public const string PatchNodeName = "SUBCONFIG";
        protected const string PatchNameKey = "__mpecPatchName";

        [KSPField(isPersistant = true)]
        public string activePatchName = null;

        [KSPField(isPersistant = true)]
        public bool dynamicPatchApplied = false;

        protected ConfigNode[] GetPatchesOfConfig(ConfigNode config) => config.GetNodes(PatchNodeName);

        protected ConfigNode GetPatch(string configName, string patchName)
        {
            return GetPatchesOfConfig(GetConfigByName(configName))
                .FirstOrDefault(patch => patch.GetValue("name") == patchName);
        }

        // TODO: This is called a lot, performance concern?
        protected ConfigNode PatchConfig(ConfigNode parentConfig, ConfigNode patch)
        {
            var patchedNode = parentConfig.CreateCopy();
            // TODO: Check if this handles multiple keys/values properly.
            patch.CopyTo(patchedNode, overwrite: true);
            patchedNode.SetValue("name", parentConfig.GetValue("name"));
            patchedNode.AddValue(PatchNameKey, patch.GetValue("name"));
            return patchedNode;
        }

        public void ApplyDynamicPatch(ConfigNode patch)
        {
            Debug.Log($"**RFMPEC** dynamic patch applied to active config `{configurationDisplay}`");
            SetConfiguration(PatchConfig(config, patch), false);
            dynamicPatchApplied = true;
        }

        protected override ConfigNode GetSetConfigurationTarget(string newConfiguration)
        {
            if (string.IsNullOrEmpty(activePatchName))
                return base.GetSetConfigurationTarget(newConfiguration);
            return PatchConfig(GetConfigByName(newConfiguration), GetPatch(newConfiguration, activePatchName));
        }

        public override void SetConfiguration(string newConfiguration = null, bool resetTechLevels = false)
        {
            base.SetConfiguration(newConfiguration, resetTechLevels);
            if (dynamicPatchApplied)
            {
                dynamicPatchApplied = false;
                part.SendMessage("OnMPECDynamicPatchOverwritten", SendMessageOptions.DontRequireReceiver);
            }
        }

        public override string GetConfigDisplayName(ConfigNode node)
        {
            var name = node.GetValue("name");
            if (!node.HasValue(PatchNameKey))
                return name;
            return $"{name} [Subconfig {node.GetValue(PatchNameKey)}]";
        }

        protected override void DrawConfigSelectors()
        {
            foreach (var node in configs)
            {
                DrawSelectButton(
                    node,
                    node.GetValue("name") == configuration && string.IsNullOrEmpty(activePatchName),
                    (configName) =>
                    {
                        activePatchName = null;
                        GUIApplyConfig(configName);
                    });
                foreach (var patch in GetPatchesOfConfig(node))
                {
                    var patchedNode = PatchConfig(node, patch);
                    string patchName = patch.GetValue("name");
                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.Space(30);
                        DrawSelectButton(
                            patchedNode,
                            node.GetValue("name") == configuration && patchName == activePatchName,
                            (configName) =>
                            {
                                activePatchName = patchName;
                                GUIApplyConfig(configName);
                            });
                    }
                }
            }
        }
    }
}
