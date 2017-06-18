﻿using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Microsoft.Xna.Framework;

namespace Barotrauma.Items.Components
{
    partial class StatusHUD : ItemComponent
    {
        private static readonly string[] BleedingTexts = {"Minor bleeding", "Bleeding", "Bleeding heavily", "Catastrophic Bleeding"};

        private static readonly string[] HealthTexts = { "No visible injuries", "Minor injuries", "Injured", "Major injuries", "Critically injured" };

        private static readonly string[] OxygenTexts = { "Oxygen level normal", "Gasping for air", "Signs of oxygen deprivation", "Not breathing" };
        
        public StatusHUD(Item item, XElement element)
            : base(item, element)
        {
        }
    }
}
