﻿using IPA;
using UnityEngine;
using IPALogger = IPA.Logging.Logger;

namespace ExtraMenuInputs
{

    [Plugin(RuntimeOptions.SingleStartInit)]
    public class Plugin
    {
        internal static Plugin instance { get; private set; }
        internal static string Name => "ExtraMenuInputs";

        [Init]
        /// <summary>
        /// Called when the plugin is first loaded by IPA (either when the game starts or when the plugin is enabled if it starts disabled).
        /// [Init] methods that use a Constructor or called before regular methods like InitWithConfig.
        /// Only use [Init] with one Constructor.
        /// </summary>
        public void Init(IPALogger logger)
        {
            instance = this;
            Logger.log = logger;
            Logger.log.Debug("Logger initialized.");
        }

        #region BSIPA Config
        //Uncomment to use BSIPA's config
        /*
        [Init]
        public void InitWithConfig(Config conf)
        {
            Configuration.PluginConfig.Instance = conf.Generated<Configuration.PluginConfig>();
            Logger.log.Debug("Config loaded");
        }
        */
        #endregion

        [OnStart]
        public void OnApplicationStart()
        {
            new GameObject("ExtraMenuInputsController").AddComponent<ExtraMenuInputsController>();

        }
    }
}
