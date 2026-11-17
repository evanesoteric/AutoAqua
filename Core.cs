using System;
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(AutoAqua.Core), "AutoAqua", "1.0.0", "Ev")]
[assembly: MelonGame("Questing Goose Studio", "Probably Stolen")]

namespace AutoAqua
{
    public class Core : MelonMod
    {
        private float _reportAt = -1f;
        private KeyCode _key = KeyCode.None;
        private bool _keyBroken;

        public override void OnInitializeMelon()
        {
            Logic.InitPrefs();

            var name = Logic.TriggerKey.Value;
            if (!string.IsNullOrEmpty(name))
            {
                try { _key = (KeyCode)Enum.Parse(typeof(KeyCode), name, true); }
                catch { LoggerInstance.Warning("TriggerKey '" + name + "' is not a valid key name; hotkey disabled"); }
            }

            LoggerInstance.Msg("AutoAqua 1.0.0 loaded (DryRun=" + Logic.DryRun.Value +
                               ", hotkey=" + (_key == KeyCode.None ? "off" : _key.ToString()) + ")");
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            // Optional setup report, 45 seconds after the shop scene loads.
            if (Logic.Report.Value && sceneName == "InventoryScene")
                _reportAt = Time.realtimeSinceStartup + 45f;
        }

        public override void OnUpdate()
        {
            if (_reportAt > 0f && Time.realtimeSinceStartup >= _reportAt)
            {
                _reportAt = -1f;
                Logic.LogReport();
            }

            if (_key != KeyCode.None && !_keyBroken)
            {
                try
                {
                    if (Input.GetKeyDown(_key))
                    {
                        LoggerInstance.Msg("[manual] " + _key + " pressed, running routine now");
                        Logic.RunMorning();
                    }
                }
                catch (Exception e)
                {
                    _keyBroken = true;
                    LoggerInstance.Warning("hotkey disabled: " + e.Message);
                }
            }
        }

        public override void OnApplicationQuit()
        {
            if (Logic.Verbose.Value) LoggerInstance.Msg("quit hook reached");
        }
    }
}
