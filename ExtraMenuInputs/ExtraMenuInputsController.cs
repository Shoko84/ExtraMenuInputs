using System;
using System.Linq;
using BS_Utils.Utilities;
using HMUI;
using UnityEngine;
using UnityEngine.XR;

namespace ExtraMenuInputs
{
    /// <summary>
    /// Monobehaviours (scripts) are added to GameObjects.
    /// For a full list of Messages a Monobehaviour can receive from the game, see https://docs.unity3d.com/ScriptReference/MonoBehaviour.html.
    /// </summary>
    public class ExtraMenuInputsController : MonoBehaviour
    {
        private enum ControllerType
        {
            None,
            Left,
            Right
        }

        #region Properties

        public static ExtraMenuInputsController instance { get; private set; }

        private TableView _currentTableViewContent;
        private TableViewScroller _currentTableViewScroller;

        private DateTime _clickDateTime;
        private ControllerType _latestClick;

        private bool _isInitialized;

        #endregion

        #region Events

        private void OnMenuSceneLoadedFresh(ScenesTransitionSetupDataSO obj)
        {
            var mmvc = Resources.FindObjectsOfTypeAll<MainMenuViewController>().FirstOrDefault();
            if (mmvc == null) return;
            mmvc.didFinishEvent += OnDidFinishEvent;
            //mmvc.didDeactivateEvent //TODO: To nullify properties when being out of the song selection? Doesn't seem necessary
        }

        private void OnDidFinishEvent(MainMenuViewController mmvc, MainMenuViewController.MenuButton menuButton)
        {
            if (menuButton != MainMenuViewController.MenuButton.SoloFreePlay &&
                menuButton != MainMenuViewController.MenuButton.Party) return;
            _clickDateTime = new DateTime(0);
            _latestClick = ControllerType.None;
            var lcvc = Resources.FindObjectsOfTypeAll<LevelCollectionViewController>().FirstOrDefault();
            if (lcvc == null) return;
            var lctv = lcvc.GetPrivateField<LevelCollectionTableView>("_levelCollectionTableView");
            if (lctv == null) return;
            _currentTableViewContent = lctv.GetPrivateField<TableView>("_tableView");
            if (_currentTableViewContent == null) return;
            Logger.log.Debug("Content table view found!");
            _genericLeftController = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            _genericRightController = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            _isInitialized = true;
        }

        #endregion

        #region Unity methods

        /// <summary>
        /// Only ever called once, mainly used to initialize variables.
        /// </summary>
        private void Awake()
        {
            // For this particular MonoBehaviour, we only want one instance to exist at any time, so store a reference to it in a static property
            //   and destroy any that are created while one already exists.
            if (instance != null)
            {
                Logger.log?.Warn($"Instance of {this.GetType().Name} already exists, destroying.");
                GameObject.DestroyImmediate(this);
                return;
            }

            GameObject.DontDestroyOnLoad(this); // Don't destroy this object on scene changes
            instance = this;
            Logger.log?.Debug($"{name}: Awake()");
        }

        /// <summary>
        /// Only ever called once on the first frame the script is Enabled. Start is called after any other script's Awake() and before Update().
        /// </summary>
        private void Start()
        {
            BSEvents.lateMenuSceneLoadedFresh += OnMenuSceneLoadedFresh;
            _clickDateTime = new DateTime(0);
        }

        private void Update()
        {
            CheckInputs();
        }


        private InputDevice _genericLeftController;
        private InputDevice _genericRightController;

        private Vector2 _leftPadAxis;
        private Vector2 _rightPadAxis;

        private bool _leftPadPress;
        private bool _rightPadPress;

        private void CheckInputs()
        {
            if (!_isInitialized || !_genericLeftController.isValid || !_genericRightController.isValid) return;
            var conManu = _genericLeftController.manufacturer;

            _genericLeftController.TryGetFeatureValue(CommonUsages.primary2DAxis, out _leftPadAxis);
            _genericRightController.TryGetFeatureValue(CommonUsages.primary2DAxis, out _rightPadAxis);

            _genericLeftController.TryGetFeatureValue(CommonUsages.primary2DAxisClick, out _leftPadPress);
            _genericRightController.TryGetFeatureValue(CommonUsages.primary2DAxisClick, out _rightPadPress);

            const float threshold = 0.2f;
            var currentTime = DateTime.Now;

            if (_latestClick != ControllerType.Left ||
                (currentTime - _clickDateTime).TotalSeconds >= 0.5f &&
                _latestClick == ControllerType.Left)
            {
                if (Math.Sign(_leftPadAxis.y) > threshold &&
                    ((conManu.Equals("Valve") || conManu.Equals("HTC")) && _leftPadPress || conManu.Equals("Oculus")))
                {
                    //Up
                    MakePageScrollUp();
                    _clickDateTime = currentTime;
                    _latestClick = ControllerType.Left;
                }
                else if (Math.Sign(_leftPadAxis.y) < -threshold &&
                         ((conManu.Equals("Valve") || conManu.Equals("HTC")) && _leftPadPress ||
                          conManu.Equals("Oculus")))
                {
                    //Down
                    MakePageScrollDown();
                    _clickDateTime = currentTime;
                    _latestClick = ControllerType.Left;
                }
            }

            if (_latestClick != ControllerType.Right ||
                (currentTime - _clickDateTime).TotalSeconds >= 0.5f &&
                _latestClick == ControllerType.Right)
            {
                if (Math.Sign(_rightPadAxis.y) > threshold &&
                    ((conManu.Equals("Valve") || conManu.Equals("HTC")) && _rightPadPress || conManu.Equals("Oculus")))
                {
                    //Up
                    MakePageScrollUp();
                    _clickDateTime = currentTime;
                    _latestClick = ControllerType.Right;
                }
                else if (Math.Sign(_rightPadAxis.y) < -threshold &&
                         ((conManu.Equals("Valve") || conManu.Equals("HTC")) && _rightPadPress ||
                          conManu.Equals("Oculus")))
                {
                    //Down
                    MakePageScrollDown();
                    _clickDateTime = currentTime;
                    _latestClick = ControllerType.Right;
                }
            }

            if (conManu.Equals("Oculus") && (Math.Sign(_leftPadAxis.y) <= threshold &&
                                             Math.Sign(_leftPadAxis.y) >= -threshold &&
                                             _latestClick == ControllerType.Left ||
                                             Math.Sign(_rightPadAxis.y) <= threshold &&
                                             Math.Sign(_rightPadAxis.y) >= -threshold &&
                                             _latestClick == ControllerType.Right) ||
                (conManu.Equals("Valve") || conManu.Equals("HTC")) &&
                (!_leftPadPress && _latestClick == ControllerType.Left ||
                 !_rightPadPress && _latestClick == ControllerType.Right))
            {
                _latestClick = ControllerType.None;
                _clickDateTime = new DateTime(0);
            }
        }

        /// <summary>
        /// Called when the script is being destroyed.
        /// </summary>
        private void OnDestroy()
        {
            Logger.log?.Debug($"{name}: OnDestroy()");
            instance = null; // This MonoBehaviour is being destroyed, so set the static instance property to null.
        }

        #endregion

        #region Methods

        private void MakePageScrollUp()
        {
            if (_currentTableViewContent == null) return;
            if (_currentTableViewScroller == null)
                _currentTableViewScroller = _currentTableViewContent.GetPrivateField<TableViewScroller>("_scroller");
            if (_currentTableViewScroller == null) return;
            _currentTableViewScroller.PageScrollUp();
            _currentTableViewContent.RefreshScrollButtons(true);
        }

        private void MakePageScrollDown()
        {
            if (_currentTableViewContent == null) return;
            if (_currentTableViewScroller == null)
                _currentTableViewScroller = _currentTableViewContent.GetPrivateField<TableViewScroller>("_scroller");
            if (_currentTableViewScroller == null) return;
            _currentTableViewScroller.PageScrollDown();
            _currentTableViewContent.RefreshScrollButtons(true);
        }

        #endregion
    }
}