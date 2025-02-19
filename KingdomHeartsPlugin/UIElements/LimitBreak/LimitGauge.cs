﻿using System;
using System.IO;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using ImGuiScene;
using KingdomHeartsPlugin.Utilities;

namespace KingdomHeartsPlugin.UIElements.LimitBreak
{
    public class LimitGauge
    {

        private TextureWrap _gaugeBackgroundTexture, _gaugeForegroundTexture, _gaugeForegroundColorlessTexture, _maxLimitTexture, _limitTextTexture, _orbTexture;

        private TextureWrap[] _numbers;

        private Orb[] _orbs;

        private int _lastLimitLevel;

        private float _maxAnimScale;

        public LimitGauge()
        {
            LimitBreakBarWidth = new int[5];
            _gaugeBackgroundTexture = KingdomHeartsPlugin.Pi.UiBuilder.LoadImage(Path.Combine(KingdomHeartsPlugin.TemplateLocation, @"Textures\LimitGauge\background.png"));
            _gaugeForegroundTexture = KingdomHeartsPlugin.Pi.UiBuilder.LoadImage(Path.Combine(KingdomHeartsPlugin.TemplateLocation, @"Textures\LimitGauge\gauge_colored.png"));
            _gaugeForegroundColorlessTexture = KingdomHeartsPlugin.Pi.UiBuilder.LoadImage(Path.Combine(KingdomHeartsPlugin.TemplateLocation, @"Textures\LimitGauge\gauge_white.png"));
            _limitTextTexture = KingdomHeartsPlugin.Pi.UiBuilder.LoadImage(Path.Combine(KingdomHeartsPlugin.TemplateLocation, @"Textures\LimitGauge\limit_text.png"));
            _maxLimitTexture = KingdomHeartsPlugin.Pi.UiBuilder.LoadImage(Path.Combine(KingdomHeartsPlugin.TemplateLocation, @"Textures\LimitGauge\max.png"));
            _orbTexture = KingdomHeartsPlugin.Pi.UiBuilder.LoadImage(Path.Combine(KingdomHeartsPlugin.TemplateLocation, @"Textures\LimitGauge\orb.png"));
            _numbers = new[]
            {
                KingdomHeartsPlugin.Pi.UiBuilder.LoadImage(Path.Combine(KingdomHeartsPlugin.TemplateLocation, @"Textures\LimitGauge\number_0.png")),
                KingdomHeartsPlugin.Pi.UiBuilder.LoadImage(Path.Combine(KingdomHeartsPlugin.TemplateLocation, @"Textures\LimitGauge\number_1.png")),
                KingdomHeartsPlugin.Pi.UiBuilder.LoadImage(Path.Combine(KingdomHeartsPlugin.TemplateLocation, @"Textures\LimitGauge\number_2.png")),
                KingdomHeartsPlugin.Pi.UiBuilder.LoadImage(Path.Combine(KingdomHeartsPlugin.TemplateLocation, @"Textures\LimitGauge\number_3.png")),
                KingdomHeartsPlugin.Pi.UiBuilder.LoadImage(Path.Combine(KingdomHeartsPlugin.TemplateLocation, @"Textures\LimitGauge\number_4.png")),
                KingdomHeartsPlugin.Pi.UiBuilder.LoadImage(Path.Combine(KingdomHeartsPlugin.TemplateLocation, @"Textures\LimitGauge\number_5.png"))
            };
            _orbs = new Orb[5];
            for (int i = 0; i < 5; i++)
            {
                _orbs[i] = new Orb();
            }

            ResetOrbs();
        }

        private unsafe bool UpdateLimitBreak()
        {
            //Get Limit Break Bar
            var LBWidget = (AtkUnitBase*)KingdomHeartsPlugin.Gui.GetAddonByName("_LimitBreak", 1);
            //Get Compressed Aether Bar
            var CAWidget = (AtkUnitBase*)KingdomHeartsPlugin.Gui.GetAddonByName("HWDAetherGauge", 1);

            var foundCaGauge = false;

            LimitBreakMaxLevel = 1;
            MaxLimitBarWidth = 128;

            // Diadem Compatibility
            if (CAWidget != null && KingdomHeartsPlugin.Ui.Configuration.LimitGaugeDiadem)
            {
                if (CAWidget->UldManager.NodeListCount == 10)
                {
                    if ((CAWidget->UldManager.SearchNodeById(3)->Alpha_2 > 0 && CAWidget->UldManager.SearchNodeById(3)->IsVisible) || KingdomHeartsPlugin.Ui.Configuration.LimitGaugeAlwaysShow)
                    {
                        var usedAuger = CAWidget->UldManager.SearchNodeById(10)->IsVisible;
                        for (uint i = 0; i < 5; i++)
                        {
                            var node = CAWidget->UldManager.SearchNodeById(5 + i + (usedAuger ? 1u : 0))->GetComponent()->UldManager.SearchNodeById(3);
                            LimitBreakBarWidth[i] = node->IsVisible ? node->Width - 14 : 0;
                        }

                        MaxLimitBarWidth = 80;
                        LimitBreakMaxLevel = 5;
                        foundCaGauge = true;
                    }
                }
            }

            if (!foundCaGauge)
            {
                // Get LB Width
                if (LBWidget != null)
                {
                    if (LBWidget->UldManager.NodeListCount == 6)
                    {
                        if ((LBWidget->UldManager.SearchNodeById(3)->Alpha_2 == 0 || !LBWidget->UldManager.SearchNodeById(3)->IsVisible) &&
                            !KingdomHeartsPlugin.Ui.Configuration.LimitGaugeAlwaysShow) return false;

                        for (uint i = 0; i < 3; i++)
                        {
                            LimitBreakBarWidth[i] = LBWidget->UldManager.SearchNodeById(6 - i)->GetComponent()->UldManager.SearchNodeById(3)->Width - 18;

                            if (LBWidget->UldManager.SearchNodeById(6 - i)->IsVisible && i > 0) LimitBreakMaxLevel++;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }

            // Set Limit Break Level
            LimitBreakLevel = 0;

            foreach (var barWidth in LimitBreakBarWidth)
            {
                if (barWidth == MaxLimitBarWidth) LimitBreakLevel++;
            }

            if (_lastLimitLevel != LimitBreakLevel)
                ResetOrbs();

            _lastLimitLevel = LimitBreakLevel;

            if (LimitBreakLevel == LimitBreakMaxLevel)
            {
                if (_maxAnimScale == 0)
                    _maxAnimScale = 2.5f;
            }
            else
            {
                _maxAnimScale = 0f;
            }

            // MAX icon animation
            if (_maxAnimScale > 1)
            {
                _maxAnimScale -= 3 * KingdomHeartsPlugin.UiSpeed;
                if (_maxAnimScale <= 1) _maxAnimScale = 1;
            }

            UpdateOrbs();
            

            return true;
        }

        public void Draw()
        {
            if (!UpdateLimitBreak()) return;

            var drawList = ImGui.GetWindowDrawList();
            var basePosition = new Vector2(KingdomHeartsPlugin.Ui.Configuration.LimitGaugePositionX, KingdomHeartsPlugin.Ui.Configuration.LimitGaugePositionY);

            // BG
            ImageDrawing.DrawImage(drawList, _gaugeBackgroundTexture, basePosition);
            // Numbers
            ImageDrawing.DrawImageQuad(drawList, _numbers[LimitBreakLevel], basePosition + new Vector2(167, 1), new Vector2(30, 0), new Vector2(30, 0), Vector2.Zero, Vector2.Zero,
                ImGui.GetColorU32(new Vector4(1, 0.4f, 0, 1)));
            // Foreground
            ImageDrawing.DrawImage(drawList, _gaugeForegroundTexture, basePosition + new Vector2(4, 4),
                new Vector4(0, 0, LimitBreakLevel == LimitBreakMaxLevel ? 1 : LimitBreakBarWidth[LimitBreakLevel] / (float)MaxLimitBarWidth, 1));
            // Text
            ImageDrawing.DrawImage(drawList, _limitTextTexture, basePosition + new Vector2(-60, 28), ImGui.GetColorU32(new Vector4(1, 0.75f, 0, 1)));
            // MAX icon
            ImageDrawing.DrawImageRotated(drawList, _maxLimitTexture, basePosition + new Vector2(95, 28), new Vector2(_maxLimitTexture.Width * _maxAnimScale, _maxLimitTexture.Height * _maxAnimScale),
                (_maxAnimScale - 1) * (float)Math.PI * 2 / 3, ImGui.GetColorU32(new Vector4(1, 1, 1, Math.Max(2 - _maxAnimScale, 0))));
            // Orbs
            for (int i = 0; i < LimitBreakLevel; i++)
            {
                ImageDrawing.DrawImage(drawList, _orbTexture, basePosition + _orbs[i].Position + new Vector2(192, 14), ImGui.GetColorU32(new Vector4(1, 0.5f, 0, _orbs[i].Alpha)));
            }
        }

        public void UpdateOrbs()
        {
            var radius = 30f;

            for (int i = 0; i < LimitBreakLevel; i++)
            {
                var direction = _orbs[i].Angle;

                float pointX = (float)(Math.Cos(_orbs[i].Angle * Math.PI / 180) * radius * 0.9 + Math.Sin(-direction * Math.PI / 180) * radius * 0.3);

                float pointy = (float)(Math.Sin(_orbs[i].Angle * Math.PI / 180) * radius);

                _orbs[i].Angle += 225f * KingdomHeartsPlugin.UiSpeed;

                if (_orbs[i].Angle >= 360)
                {
                    _orbs[i].Angle -= 360;
                }

                if (_orbs[i].Angle >= -160 && _orbs[i].Alpha < 1)
                {
                    _orbs[i].Alpha += 1.5f * KingdomHeartsPlugin.UiSpeed;
                }

                _orbs[i].Position = new Vector2(pointX - 1, pointy);
            }
        }

        public void ResetOrbs()
        {
            for (int i = 0; i < 5; i++)
            {
                _orbs[i].Position = Vector2.Zero;
                _orbs[i].Alpha = 0;
                _orbs[i].Angle = -200 + -i * (360 / (Math.Max(LimitBreakLevel, 1)));
            }
        }

        public void Dispose()
        {
            _gaugeBackgroundTexture.Dispose();
            _gaugeForegroundColorlessTexture.Dispose();
            _gaugeForegroundTexture.Dispose();
            _limitTextTexture.Dispose();
            _maxLimitTexture.Dispose();
            _orbTexture.Dispose();

            for (int i = 0; i < _numbers.Length; i++)
            {
                _numbers[i].Dispose();
                _numbers[i] = null;
            }

            _gaugeBackgroundTexture = null;
            _gaugeForegroundColorlessTexture = null;
            _gaugeForegroundTexture = null;
            _limitTextTexture = null;
            _maxLimitTexture = null;
            _orbTexture = null;
        }

        private int LimitBreakLevel { get; set; }
        private int LimitBreakMaxLevel { get; set; }
        private int[] LimitBreakBarWidth { get; }
        private int MaxLimitBarWidth { get; set; }
    }
}
