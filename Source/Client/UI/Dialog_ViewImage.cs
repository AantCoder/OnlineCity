using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OCUnion;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimWorldOnlineCity.UI
{
    public static class Constants
    {
        public const float AbsoluteMaxZoomLevel = 3f;
        public const float ZoomStep = .05f;
    }

    internal class Dialog_ViewImage : MainTabWindow
    {
        public IntVec2 Size = new IntVec2(400, 400);
        public Texture2D ImageShow = null;
        public string TextShow = null;
        public bool TextShowOnUp = false;
        public Action BeforeDrow = null;

        internal static Vector2 _scrollPosition = Vector2.zero;

        private Rect _treeRect;

        private Rect _baseViewRect;
        private Rect _baseViewRect_Inner;

        private Vector2 _mousePosition = Vector2.zero;

        private Rect _viewRect;

        private Rect _viewRect_Inner;
        private bool _viewRect_InnerDirty = true;
        private bool _viewRectDirty = true;

        private float _zoomLevel = 1f;

        public Dialog_ViewImage()
        {
            doCloseX = true;

            closeOnClickedOutside = false;

            TextShow = "OC_Loading".Translate() + "...";
        }

        public float ZoomLevel
        {
            get => _zoomLevel;
            set
            {
                _zoomLevel = Mathf.Clamp(value, 1f, MaxZoomLevel);
                _viewRectDirty = true;
                _viewRect_InnerDirty = true;
            }
        }

        public Rect ViewRect
        {
            get
            {
                if (_viewRectDirty)
                {
                    _viewRect = new Rect(
                        _baseViewRect.xMin * ZoomLevel,
                        _baseViewRect.yMin * ZoomLevel,
                        _baseViewRect.width * ZoomLevel,
                        _baseViewRect.height * ZoomLevel
                    );
                    _viewRectDirty = false;
                }

                return _viewRect;
            }
        }

        public Rect ViewRect_Inner
        {
            get
            {
                if (_viewRect_InnerDirty)
                {
                    _viewRect_Inner = _viewRect.ContractedBy(Margin * ZoomLevel);
                    _viewRect_InnerDirty = false;
                }

                return _viewRect_Inner;
            }
        }

        public Rect TreeRect
        {
            get
            {
                if (_treeRect == default)
                {
                    var width = Size.x;
                    var height = Size.z;
                    _treeRect = new Rect(0f, 0f, width, height);
                }

                return _treeRect;
            }
        }

        internal float MaxZoomLevel
        {
            get
            {
                // get the minimum zoom level at which the entire tree fits onto the screen, or a static maximum zoom level.
                var fitZoomLevel = Mathf.Min(TreeRect.width / _baseViewRect_Inner.width,
                                              TreeRect.height / _baseViewRect_Inner.height);
                return Mathf.Min(fitZoomLevel, Constants.AbsoluteMaxZoomLevel);
            }
        }

        public override void PreClose()
        {
            base.PreClose();
        }

        public override void PostOpen()
        {
            try
            {
                base.PostOpen();
            }
            catch
            { }
        }

        public override void PreOpen()
        {
            base.PreOpen();
            try
            {
                SetRects();
            }
            catch
            { }

            forcePause = true;

            _scrollPosition = Vector2.zero;
            ZoomLevel = 1f;

            closeOnClickedOutside = false;
            this.onlyOneOfTypeAllowed = true;
            this.preventCameraMotion = true;
        }

        private void SetRects()
        {
            // tree view rects, have to deal with UIScale and ZoomLevel manually.
            _baseViewRect = new Rect(
                0f,
                0f,
                Screen.width / Prefs.UIScale,
                (Screen.height - MainButtonDef.ButtonHeight) / Prefs.UIScale);
            _baseViewRect_Inner = _baseViewRect;

            // windowrect, set to topleft (for some reason vanilla alignment overlaps bottom buttons).
            windowRect.x = 0f;
            windowRect.y = 0f;
            windowRect.width = Verse.UI.screenWidth;
            windowRect.height = Verse.UI.screenHeight - MainButtonDef.ButtonHeight;
        }

        public override void DoWindowContents(Rect canvas)
        {
            if (BeforeDrow != null) BeforeDrow();

            if (ImageShow != null && 
                (Size.x != ImageShow.width || Size.z != ImageShow.height))
            {
                Size = new IntVec2(ImageShow.width, ImageShow.height);
                _treeRect = default;
                ZoomLevel = ZoomLevel;
                _scrollPosition.x = (TreeRect.width - ViewRect.width) / 2f;
                _scrollPosition.y = (TreeRect.height - ViewRect.height) / 2f;
            }

            if (ImageShow != null)
            {
                ApplyZoomLevel();

                // draw background
                //GUI.DrawTexture(ViewRect, Assets.SlightlyDarkBackground);

                // draw the actual tree
                _scrollPosition = GUI.BeginScrollView(ViewRect, _scrollPosition, TreeRect);
                GUI.BeginGroup(new Rect(0, 0, TreeRect.width, TreeRect.height));

                var rectOut = new Rect(TreeRect);
                if (rectOut.width < ViewRect_Inner.width) rectOut.x += (ViewRect_Inner.width - rectOut.width) / 2f;
                if (rectOut.height < ViewRect_Inner.height) rectOut.y += (ViewRect_Inner.height - rectOut.height) / 2f;
                //Loger.Log($"WI scrollPos={_scrollPosition} Zoom={ZoomLevel} MaxZoom={MaxZoomLevel} ViewRect={ViewRect} TreeRect={TreeRect} image=({ImageShow?.width}, {ImageShow?.height})");

                GUI.DrawTexture(rectOut, ImageShow);

                HandleZoom();

                GUI.EndGroup();
                GUI.EndScrollView(false);

                HandleDragging();
                HandleDolly();

                // reset zoom level
                ResetZoomLevel();
            }

            if (ImageShow == null || TextShowOnUp)
            {
                GUI.color = Color.white;
                Text.Font = GameFont.Medium;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(canvas, TextShow);
            }
            // cleanup;
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private void HandleDolly()
        {
            var dollySpeed = 10f;
            if (KeyBindingDefOf.MapDolly_Left.IsDown)
                _scrollPosition.x -= dollySpeed;
            if (KeyBindingDefOf.MapDolly_Right.IsDown)
                _scrollPosition.x += dollySpeed;
            if (KeyBindingDefOf.MapDolly_Up.IsDown)
                _scrollPosition.y -= dollySpeed;
            if (KeyBindingDefOf.MapDolly_Down.IsDown)
                _scrollPosition.y += dollySpeed;
        }


        void HandleZoom()
        {
            // handle zoom only with shift
            if (Event.current.isScrollWheel && true/*Event.current.shift*/)
            {
                // absolute position of mouse on research tree
                var absPos = Event.current.mousePosition;
                // Log.Debug( "Absolute position: {0}", absPos );

                // relative normalized position of mouse on visible tree
                var relPos = (Event.current.mousePosition - _scrollPosition) / ZoomLevel;
                // Log.Debug( "Normalized position: {0}", relPos );

                // update zoom level
                ZoomLevel += Event.current.delta.y * Constants.ZoomStep * ZoomLevel;

                // we want to keep the _normalized_ relative position the same as before zooming
                _scrollPosition = absPos - relPos * ZoomLevel;

                Event.current.Use();
            }
        }

        void HandleDragging()
        {
            // middle mouse or holding down shift for panning
            if (Event.current.button == 2 || true/*Event.current.shift*/)
            {
                if (Event.current.type == EventType.MouseDown)
                {
                    _mousePosition = Event.current.mousePosition;
                    Event.current.Use();
                }
                if (Event.current.type == EventType.MouseUp)
                {
                    _mousePosition = Vector2.zero;
                }
                if (Event.current.type == EventType.MouseDrag)
                {
                    var _currentMousePosition = Event.current.mousePosition;
                    _scrollPosition += _mousePosition - _currentMousePosition;
                    _mousePosition = _currentMousePosition;
                }
            }
            // scroll wheel vertical, switch to horizontal with alt
            if (Event.current.isScrollWheel && !true/*Event.current.shift*/)
            {
                float delta = Event.current.delta.y * 15;
                if (Event.current.alt)
                {
                    _scrollPosition.x += delta;
                }
                else
                {
                    _scrollPosition.y += delta;
                }
            }
        }

        private void ApplyZoomLevel()
        {
            GUI.EndClip(); // window contents
            GUI.EndClip(); // window itself?
            GUI.matrix = Matrix4x4.TRS(new Vector3(0f, 0f, 0f), Quaternion.identity,
                                        new Vector3(Prefs.UIScale / ZoomLevel, Prefs.UIScale / ZoomLevel, 1f));
        }

        private void ResetZoomLevel()
        {
            // dummies to maintain correct stack size
            // TO DO; figure out how to get actual clipping rects in ApplyZoomLevel();
            Verse.UI.ApplyUIScale();
            GUI.BeginClip(windowRect);
            GUI.BeginClip(new Rect(0f, 0f, Verse.UI.screenWidth, Verse.UI.screenHeight));
        }

    }
}
