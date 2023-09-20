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
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

//This namespace holds Strategies in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Strategies
{
	public class BarUpDown : Strategy
	{
		private int		breakEvenTicks		= 20;		// Default setting for ticks needed to acheive before stop moves to breakeven		
		private int		plusBreakEven		= 10; 		// Default setting for amount of ticks past breakeven to actually breakeven
		private int 	BarTraded 			= 0; 		// Default setting for Bar number that trade occurs	
		private double	initialBreakEven	= 0; 		// Default setting for where you set the breakeven
		private double 	previousPrice		= 0;		// previous price used to calculate trailing stop
		private double 	newPrice			= 0;		// Default setting for new price used to calculate trailing stop
		private 		HMA hmaHigh;
		private double	adxValue 			= 0 ;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Tries to ride a trend using HA candles with a confirmation of 2/3 candles";
				Name										= "BarUpDown";
				Calculate									= Calculate.OnBarClose;
				EntriesPerDirection							= 1;
				EntryHandling								= EntryHandling.AllEntries;
				IsExitOnSessionCloseStrategy				= true;
				ExitOnSessionCloseSeconds					= 60;
				IsFillLimitOnTouch							= false;
				MaximumBarsLookBack							= MaximumBarsLookBack.TwoHundredFiftySix;
				OrderFillResolution							= OrderFillResolution.Standard;
				Slippage									= 0;
				StartBehavior								= StartBehavior.WaitUntilFlat;
				TimeInForce									= TimeInForce.Gtc;
				TraceOrders									= false;
				RealtimeErrorHandling						= RealtimeErrorHandling.StopCancelClose;
				StopTargetHandling							= StopTargetHandling.PerEntryExecution;
				BarsRequiredToTrade							= 20;
				// Disable this property for performance gains in Strategy Analyzer optimizations
				// See the Help Guide for additional information
				IsInstantiatedOnEachOptimizationIteration	= true;
				LongTermMAPeriod							= 81;
				Use3Candles									= true;
				EnableTimeOfDay								= true;
				Startime									= DateTime.Parse("23:00", System.Globalization.CultureInfo.InvariantCulture);
				EndTime										= DateTime.Parse("13:00", System.Globalization.CultureInfo.InvariantCulture);

				UseATRforStopLoss							= false;
				adxPeriod									= 14;
				useAdx										= false ;
				adxThreshold								= 25 ;
	
				
				//breakEvenTicks		= 4;		// Default setting for ticks needed to acheive before stop moves to breakeven		
				//plusBreakEven		= 2; 		// Default setting for amount of ticks past breakeven to actually breakeven
				profitTargetTicks	= 80;		// Default setting for how many Ticks away from AvgPrice is profit target
		        stopLossTicks		= 40;		// Default setting for stoploss. Ticks away from AvgPrice		
				trailProfitTrigger	= 20;		// 8 Default Setting for trail trigger ie the number of ticks movede after break even befor activating TrailStep
				trailStepTicks		= 8;		// 2 Default setting for number of ticks advanced in the trails - take into consideration the barsize as is calculated/advanced next bar
			}
				
			else if (State == State.Configure)
			{
				AddDataSeries(BarsPeriodType.Minute,2);
				SetStopLoss(CalculationMode.Ticks, stopLossTicks);
				SetProfitTarget(CalculationMode.Ticks, profitTargetTicks);
			}
			else if (State == State.DataLoaded)
			{
				
				// Add simple moving averages to the chart for display
				// This only displays the SMA's for the primary Bars object on the chart
				// Note only indicators based on the charts primary data series can be added.
				hmaHigh = HMA(LongTermMAPeriod);
				//hmaLow = HMA(50);
				AddChartIndicator(hmaHigh);
				//adxValue = useAdx ? (ADX(adxPeriod)[1]) : 0 ;
				
			}
		}
		
		private void FillLongEntry1()
		{
			Print("Entering Long");
			EnterLong(1,Convert.ToInt32(DefaultQuantity), "Long");
			BarTraded = CurrentBar;
		}
		
		private void FillShortEntry1()
		{
			Print("Entering Short");	
			EnterShort(1,Convert.ToInt32(DefaultQuantity), "Short");
			BarTraded = CurrentBar;
		}  
		
		protected override void OnBarUpdate()
		{
			if (BarsInProgress != 0) 
				return;
			// Can restrict trading on Sundays and partial Holidays and Holiday
			// Time zone used in current PC time zone. Should convert to New York time and use 
			
			bool validTimeforTrading = false;
			
			if (ToTime(Time[0]) >= 125500 && ToTime(Time[0]) <= 130000)
			{
				Print("Exiting Positions at End of Session");
				if (Position.MarketPosition == MarketPosition.Long)
					ExitLong();
				if (Position.MarketPosition == MarketPosition.Short)
					ExitShort();
			}			
			
			if(ToTime(Time[0]) <= 200000 && ToTime(Time[0]) >= 125000)
			{
				// Outside Trading hours
				// Need to figure out how to input this data properly so that it can be set different for different Symbols
				// Also need to add option of Killzones etc...
				// Left this open to test trades on simulator
				//return;
				// Exit all positions at 12:55 PST
			}

			if (CurrentBars[0] < 20)
				return;
			
			adxValue = useAdx ? (ADX(adxPeriod)[1]) : 0 ;

			//adxValue = ADX(adxPeriod)[1]; 
			
			// Resets the stop loss to the original value when all positions are closed
			switch (Position.MarketPosition)
            {
                case MarketPosition.Flat:
                    SetStopLoss(CalculationMode.Ticks, stopLossTicks);
					previousPrice = 0;
                    break;
				 case MarketPosition.Long:
                    // Once the price is greater than entry price+ breakEvenTicks ticks, set stop loss to breakeven
                    if (Close[0] > Position.AveragePrice + breakEvenTicks * TickSize
						&& previousPrice == 0 )
                    {
						initialBreakEven = Position.AveragePrice + plusBreakEven * TickSize;
                        SetStopLoss(CalculationMode.Price, initialBreakEven);
						previousPrice = Position.AveragePrice;
						Print("previousPrice = "+previousPrice);
                    }
					// Once at breakeven wait till trailProfitTrigger is reached before advancing stoploss by trailStepTicks size step
					else if (previousPrice	!= 0 ////StopLoss is at breakeven
 							&& GetCurrentAsk() > previousPrice + trailProfitTrigger * TickSize
						)
					{
						newPrice = previousPrice + trailStepTicks * TickSize;
						SetStopLoss(CalculationMode.Price, newPrice);
						previousPrice = newPrice;
						Print("previousPrice = "+previousPrice);
					}
					
					// If the channel is rising then raise the profit target higher
					if(IsRising( HMA(LongTermMAPeriod)))
					{
						//SetProfitTarget(CalculationMode.Price,  trailProfitTrigger * TickSize );
						
					}
					//Long entry will close when price (Close) goes over HF
					if(IsFalling( HMA(LongTermMAPeriod)))
					{
						//closeLongCondition = true ;
						Print("Exiting Longs with MA moving Falling");
						Draw.Diamond(this,"closeLongCondition"+Low[0].ToString(), true, 0,High[0] + TickSize, Brushes.DeepPink);
						ExitLong();
					}
					//Stop Loss will happen when price (Close) goes under LL
					
                    break;
                case MarketPosition.Short:
					
					// If the channel is faling then raise the profit target higher
					if( IsFalling( HMA(LongTermMAPeriod))
					  )
					{
						//SetProfitTarget(CalculationMode.Price,  trailProfitTrigger * TickSize );
						
					}
                    // Once the price is Less than entry price - breakEvenTicks ticks, set stop loss to breakeven
                    if (Close[0] < Position.AveragePrice - breakEvenTicks * TickSize
						&& previousPrice == 0)
                    {
						initialBreakEven = Position.AveragePrice - plusBreakEven * TickSize;
                        SetStopLoss(CalculationMode.Price, initialBreakEven);
						previousPrice = Position.AveragePrice;
						Print("previousPrice = "+previousPrice);
                    }
					// Once at breakeven wait till trailProfitTrigger is reached before advancing stoploss by trailStepTicks size step
					else if (previousPrice	!= 0 ////StopLoss is at breakeven
 							&& GetCurrentAsk() < previousPrice - trailProfitTrigger * TickSize
						)
					{
						newPrice = previousPrice - trailStepTicks * TickSize;
						//SetStopLoss(CalculationMode.Price, newPrice);
						previousPrice = newPrice;
						Print("previousPrice = "+previousPrice);
					}
					
					//Short entry will close when price (Close) goes over LF
					if(IsRising( HMA(LongTermMAPeriod))
					    )
					{
						Print("Exiting Shorts with MA moving Rising");
						//closeShortCondition = true ;
						Draw.Diamond(this,"CloseShortEntry"+Low[0].ToString(), true, 0,Low[0] - TickSize, Brushes.Green);
						ExitShort();
					}
				
				
                    break;
                default:
					//autoStopA900Traded = false;
                    break;
			}
			
			 //LONG CONDITION
			// Check Three Bars 
			// can also check for displacement etc??? sometimes that is good for exits
			//	Need to check for additional entry conditions to validate entry or to eliminate consolidations/chop
			
			if ( (Close[0] > Open[0])
				&& (Close[0] > Close[1])
				&& (Close[0] > Close[2])
				//&& (Close[1] > Close[3])
				&& IsRising(HMA(LongTermMAPeriod))
				&& (Position.MarketPosition != MarketPosition.Long) 
				&& (useAdx ?adxValue > adxThreshold:true)
				)
			{
				//if (Position.MarketPosition == MarketPosition.Short) ExitShort();
					
				//longCondition = true ;
				Draw.ArrowUp(this,"Long Entry"+Low[0].ToString(), true, 0,Low[0]-TickSize, Brushes.Gold);
					//EnterLongStopLimit(EntriesPerDirection,High[0] + 2 * TickSize, High[0]- 10* TickSize, "longCondition");
					//EnterLong();
				FillLongEntry1();
					//SetStopLoss(CalculationMode.Ticks, stopLossTicks);
				SetProfitTarget(CalculationMode.Ticks,profitTargetTicks);
				SetStopLoss(CalculationMode.Ticks, stopLossTicks);
			}
			
			// EXIT CRITERIA to IMPLEMENT
			//	When two Candles go in the opposite direction. (Check if this will reduce the losses 
			//	Or look at SD of 2.5 -> 4.5 for exit
			//	USE ATR for trailing or PSAR
			// 	Use STDDEV of HMA to validate trade entry. Should reduce the number of trades in consolidation or low volume
			//	Antoher way is to do an EMA of Volume to validate need to enter trade
			// 	Cross over of Fast/Slow HMA or EMA will be good to exit quickly
			
			
			 // SHORT CONDITION
			if ((Open[0] > Close[0])
				 && (Open[0] < Open[1]) 
				 && (Open[0] < Open[2])
				// && (Open[1] > Open[2])
				 //&& (Open[0] < Close[3])
				 && IsFalling(HMA(LongTermMAPeriod))
				&& (Position.MarketPosition != MarketPosition.Short) 
				&& (useAdx ?adxValue > adxThreshold:true)
				)
			{
				//if (Position.MarketPosition == MarketPosition.Long) ExitLong();
					
				//longCondition = true ;
				Draw.ArrowDown(this,"Short Entry"+Low[0].ToString(), true, 0,High[0] + TickSize, Brushes.Blue);

				FillShortEntry1();
					//SetStopLoss(CalculationMode.Ticks, stopLossTicks);
				SetProfitTarget(CalculationMode.Ticks,profitTargetTicks);
				SetStopLoss(CalculationMode.Ticks, stopLossTicks);
			}
		}

		#region Properties
		[NinjaScriptProperty]
		[Range(3, int.MaxValue)]
		[Display(Name="LongTermMAPeriod", Description="Long term MA for trend detection", Order=1, GroupName="Parameters")]
		public int LongTermMAPeriod
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="ADX Period Length", Description="ADX Period Length", Order=1, GroupName="Parameters")]
		public int adxPeriod
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Enable ADX", Description="Use ADX to restrict trades during low volatality", Order=2, GroupName="Parameters")]
		public bool useAdx
		{ get; set; }
		

		[NinjaScriptProperty]
		[Range(1, 30)]
		[Display(Name="ADX Threshold", Description="ADX Threshold to restrict trades during low volatality", Order=2, GroupName="Parameters")]
		public double adxThreshold
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name="Use3Candles", Description="Use 2 or 3 candles for confirmation", Order=2, GroupName="Parameters")]
		public bool Use3Candles
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name="EnableTimeOfDay", Description="Restrict trading during certain times", Order=3, GroupName="Parameters")]
		public bool EnableTimeOfDay
		{ get; set; }

		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="Startime", Description="Start time for executions", Order=4, GroupName="Parameters")]
		public DateTime Startime
		{ get; set; }

		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="EndTime", Description="End time for trades", Order=5, GroupName="Parameters")]
		public DateTime EndTime
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="TakeProfitTicks", Description="What is the min Profit target", Order=6, GroupName="Parameters")]
		public int profitTargetTicks
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="StopLossTicks", Description="What is the StopLoss", Order=7, GroupName="Parameters")]
		public int stopLossTicks
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name="UseATRforStopLoss", Description="Use ATR instead of ticks for stop Loss", Order=8, GroupName="Parameters")]
		public bool UseATRforStopLoss
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="TrailProfitTrigger", Description="Profit Trigger for stop loss trailing", Order=9, GroupName="Parameters")]
		public int trailProfitTrigger
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="TrailStepTicks", Description="Steps at which we will trail the profit", Order=10, GroupName="Parameters")]
		public int trailStepTicks
		{ get; set; }
		#endregion

	}
}
