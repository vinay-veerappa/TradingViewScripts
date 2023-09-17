//
// Copyright (C) 2023, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//
#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

// This namespace holds strategies in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Strategies
{
	public class AutoStrategy4 : Strategy
	{
		
		protected override void OnStateChange()
		{
			base.OnStateChange();

			if (State == State.SetDefaults)
			{
				IncludeTradeHistoryInBacktest             = false;
				IsInstantiatedOnEachOptimizationIteration = true;
				MaximumBarsLookBack                       = MaximumBarsLookBack.TwoHundredFiftySix;
				Name                                      = "AutoStrategy4";
				SupportsOptimizationGraph                 = false;
			}
			else if (State == State.Configure)
			{
				SetParabolicStop(CalculationMode.Percent, 0.005);
			}
			else if (State == State.DataLoaded)
			{
				AddChartIndicator(FisherTransform(10));
				AddChartIndicator(TSI(3, 14));

        //Strategy 2
       // AddChartIndicator(FisherTransform(9));
				//AddChartIndicator(PriceOscillator(12, 26, 9));
      // Strategy 3
      //AddChartIndicator(FisherTransform(10));
				//AddChartIndicator(PriceOscillator(12, 27, 9));
      
			}
		}

		protected override void OnBarUpdate()
		{
			base.OnBarUpdate();

			if (CurrentBars[0] < BarsRequiredToTrade)
				return;

    // Strategy 2
   // if (FisherTransform(9)[0].ApproxCompare(7.36) >= 0)
   //Strategy 1 and 3
			if (FisherTransform(10)[0].ApproxCompare(7.36) >= 0)
				EnterLong();
		}
	}
}
