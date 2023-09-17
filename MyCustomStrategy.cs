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
	public class MyCustomStrategy : Strategy
	{
		private double HL;
		private double HF;
		private double LF;
		private double LL;
		private double stopLossShort;
		private double stopLossLong;
		
		private HMA hmaHigh;
		private HMA hmaLow;
		
		private int		breakEvenTicks		= 4;		// Default setting for ticks needed to acheive before stop moves to breakeven		
		private int		plusBreakEven		= 2; 		// Default setting for amount of ticks past breakeven to actually breakeven
		private int		profitTargetTicks	= 40;		// Default setting for how many Ticks away from AvgPrice is profit target
        private int		stopLossTicks		= 40;		// Default setting for stoploss. Ticks away from AvgPrice		
		private int		trailProfitTrigger	= 10;		// 8 Default Setting for trail trigger ie the number of ticks movede after break even befor activating TrailStep
		private int		trailStepTicks		= 8;		// 2 Default setting for number of ticks advanced in the trails - take into consideration the barsize as is calculated/advanced next bar
		private int 	BarTraded 			= 0; 		// Default setting for Bar number that trade occurs		
		
		private double	initialBreakEven	= 0; 		// Default setting for where you set the breakeven
		private double 	previousPrice		= 0;		// previous price used to calculate trailing stop
		private double 	newPrice			= 0;		// Default setting for new price used to calculate trailing stop
		
		// Defines the Series object
		private Series<double> HLSeries;
		private Series<double> LLSeries;

	
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Strategy here.";
				Name										= "MyCustomStrategy";
				Calculate									= Calculate.OnBarClose;
				EntriesPerDirection							= 1;
				EntryHandling								= EntryHandling.AllEntries;
				IsExitOnSessionCloseStrategy				= true;
				ExitOnSessionCloseSeconds					= 60;
				IsFillLimitOnTouch							= false;
				MaximumBarsLookBack							= MaximumBarsLookBack.Infinite;
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
				Fibonacci_Length					= 36;
				higherTF				= 180 ;
				UseHMA					= true;
				HMA_length					= 81;
				EnableTimeOfDay					= true;
				StartTime						= DateTime.Parse("00:00", System.Globalization.CultureInfo.InvariantCulture);
				EndTime						= DateTime.Parse("13:00", System.Globalization.CultureInfo.InvariantCulture);
				HL					= 1;
				LL					= 1;
				LF 					= 1;
				HF					= 1;
				stopLossShort		= 1;
				stopLossLong		= 1;
				
				
				//ExitOnClose = false;
				
				
				AddPlot(new Stroke(Brushes.Blue, 1), PlotStyle.Line, "HL_Plot");		
				AddPlot(new Stroke(Brushes.Aqua, 1), PlotStyle.Line, "HF_Plot");
				AddPlot(new Stroke(Brushes.DarkCyan, 1), PlotStyle.Line, "LF_Plot");		
				AddPlot(new Stroke(Brushes.Blue, 1), PlotStyle.Line, "LL_Plot");
				
			}
			else if (State == State.Configure)
			{
				AddDataSeries(Data.BarsPeriodType.Minute, higherTF);
				
				// Submits a stop loss of $500
			   // SetStopLoss(CalculationMode.Price , 20);
				/* There are several ways you can use SetStopLoss and SetProfitTarget. You can have them set to a currency value
				or some sort of calculation mode. Calculation modes available are by percent, price, and ticks. SetStopLoss and
				SetProfitTarget will submit real working orders unless you decide to simulate the orders. */
				
				// 7-8-2020, changed default to CalculateOnBarClose = false; (was true) to better work with moving the stops.
				
				SetStopLoss(CalculationMode.Ticks, stopLossTicks);
				SetProfitTarget(CalculationMode.Ticks, profitTargetTicks);

				
			}
			else if (State == State.DataLoaded)
			{
				// Best practice is to instantiate indicators in State.DataLoaded.
				
				double distance = 0.0;
				HL = Highs[1][0]; // MAX(Highs[1],1)[0];
				LL = Lows[1][0];
				
				distance = HL-LL ;
				HF = HL-distance * 0.236 ; // Bottom High of the Fib channel
				LF = HL - distance * 0.764; // Top Low of the Fib channel
				
				stopLossShort =  HL + distance * 0.886 ;
				stopLossLong = LL - distance * 0.011 ;
				
				// Create a new Series object and assign it to the variable myDoubleSeries declared in the ‘Variables’ region above
				HLSeries = new Series<double>(this);
				LLSeries = new Series<double>(this);
				
				//HL = MAX(High,Fibonacci_Length)[0];
				//LL = MIN(Low,Fibonacci_Length)[0];
				
				//Print("The Highest Value is "+ HL.ToString() + " Low = "+ LL.ToString());

				// Note: Bars are added to the BarsArray and can be accessed via an index value
				// E.G. BarsArray[1] ---> Accesses the 5 minute Bars object added above


				// Add simple moving averages to the chart for display
				// This only displays the SMA's for the primary Bars object on the chart
				// Note only indicators based on the charts primary data series can be added.
				hmaHigh = HMA(HMA_length);
				//hmaLow = HMA(50);
				AddChartIndicator(hmaHigh);
				//AddChartIndicator(hmaLow);
			}
		}
		
		
		private void FillLongEntry1()
		{
			EnterLong();
			BarTraded = CurrentBar;
		}
		
		private void FillShortEntry1()
		{
			EnterShort();
			BarTraded = CurrentBar;
		}  
		
		protected override void OnBarUpdate()
		{
			if (BarsInProgress != 0) 
				return;
			
			// First check if it is EOD and close all positions
			
			
			// Can restrict trading on Sundays and partial Holidays and Holiday
			// Time zone used in current PC time zone. Should convert to New York time and use 
			
			bool validTimeforTrading = false;
			
			// Exit all positions at 12:55 PST
			if (ToTime(Time[0]) >= 130000)
			{
				if (Position.MarketPosition == MarketPosition.Long)
					ExitLong();
				if (Position.MarketPosition == MarketPosition.Short)
					ExitShort();
			}
			
			if(ToTime(Time[0]) <= 220000 && ToTime(Time[0]) >= 125000)
			{
				// Outside Trading hours
				return;
			}

			
			

			if (BarsInProgress == 0) // This is for current TF stored in index 0
			{
		
				
				double distance = 0.0;
				
				HL = MAX(BarsArray[1],Fibonacci_Length)[0]; //Highs
				LL = MIN(BarsArray[1],Fibonacci_Length)[0]; //Lows;
				
				HLSeries[0] = HL ;
				LLSeries[0] = LL ;
				
				//Print("AA The Highest Value is "+ HL.ToString() + " Low = "+ LL.ToString());
				distance = HL-LL ;
				HF = HL-distance * 0.236 ; // Bottom High of the Fib channel
				LF = HL - distance * 0.764; // Top Low of the Fib channel
				
				stopLossShort = HL  * 1.011 ;// stopLossTicks ;// HL  * 1.011 ;
				stopLossLong = HL - distance * 1.011 ; //stopLossTicks ; //HL - distance * 1.011 ;
				
				Values[0][0] = HL ;
				Values[1][0] = HF ;
				Values[2][0] = LF ;
				Values[3][0] = LL ;
				
				bool shortCondition = false;
				bool longCondition = false ;
				bool closeLongCondition = false ;
				bool closeShortCondition = false ;
				bool slShortClose = false ;
				bool slLongClose = false ;
				
				// Check the conditions for enabling HMA and requisite checks and implement them.
				// Can also look at what is happening with the Fib envelopes and use them to predict direction?
				
				//Short entry will happen when price (Close) goes over HF
				if(CrossBelow(Close , HF,1)  
					&& (LLSeries[0] == LLSeries[0] || IsFalling(LLSeries)) // Bottom Fib is Flat or falling
					&& IsRising(HLSeries) == false
					&& (Close[0] <= HMA(HMA_length)[0])  	
					&& Position.MarketPosition != MarketPosition.Short
				   )
				{
					if (Position.MarketPosition != MarketPosition.Long) ExitLong();
					
					shortCondition = true ;
					Draw.ArrowDown(this,"ShortEntry"+Low[0].ToString(), true, 0,High[0]+TickSize, Brushes.Red);
					//EnterShortStopLimit(EntriesPerDirection,High[0] - 2 * TickSize, High[0]- 10* TickSize, "ShortEntry");
					//EnterShort();
					
					FillShortEntry1();
					SetProfitTarget(CalculationMode.Price, LL);
					SetStopLoss(CalculationMode.Price, stopLossShort);
					//SetStopLoss(CalculationMode.Ticks, stopLossTicks);
				}
				
				//Long entry will happen when price (Close) goes over LF
				if(CrossAbove(Close , LF,1) 
					&& (HLSeries[0] == HLSeries[0] || IsRising(HLSeries)) // Top Fib is flat or rising
					&& IsFalling(LLSeries) == false
					//&& IsRising( HMA(HMA_length))
					//&& CrossAbove(Close , HMA(HMA_length),1)
					&& (Close[0] >= HMA(HMA_length)[0])  	
					&& Position.MarketPosition != MarketPosition.Short
				   ) 
				{	
					if (Position.MarketPosition != MarketPosition.Short) ExitShort();
					
					longCondition = true ;
					Draw.ArrowUp(this,"longCondition"+Low[0].ToString(), true, 0,Low[0] - TickSize, Brushes.Blue);
					//EnterLongStopLimit(EntriesPerDirection,High[0] + 2 * TickSize, High[0]- 10* TickSize, "longCondition");
					//EnterLong();
					FillLongEntry1();
					//SetStopLoss(CalculationMode.Ticks, stopLossTicks);
					SetProfitTarget(CalculationMode.Price, HF * 1.011);
					SetStopLoss(CalculationMode.Price, stopLossLong);
				}
				
				
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
                       //SetStopLoss(CalculationMode.Price, initialBreakEven);
						previousPrice = Position.AveragePrice;
						Print("previousPrice = "+previousPrice);
                    }
					// Once at breakeven wait till trailProfitTrigger is reached before advancing stoploss by trailStepTicks size step
					else if (previousPrice	!= 0 ////StopLoss is at breakeven
 							&& GetCurrentAsk() > previousPrice + trailProfitTrigger * TickSize
						)
					{
						newPrice = previousPrice + trailStepTicks * TickSize;
						//SetStopLoss(CalculationMode.Price, newPrice);
						previousPrice = newPrice;
						Print("previousPrice = "+previousPrice);
					}
					
					// If the channel is rising then raise the profit target higher
					if(IsRising( HLSeries) 
						|| IsRising( HMA(HMA_length))
					  )
					{
						SetProfitTarget(CalculationMode.Price, HF + trailProfitTrigger * TickSize );
						
					}
					//Long entry will close when price (Close) goes over HF
					if(CrossAbove(Close , HF,1) 
						|| IsFalling( LLSeries)
						|| IsFalling( HMA(HMA_length))
						&& (Close[0] >= HMA(HMA_length)[0])  
						)
					{
						closeLongCondition = true ;
						Draw.Diamond(this,"closeLongCondition"+Low[0].ToString(), true, 0,High[0] + TickSize, Brushes.DeepPink);
						ExitLong();
					}
					//Stop Loss will happen when price (Close) goes under LL
					if(CrossBelow(Close , stopLossLong,1))
					{
						slLongClose = true ;
						Draw.TriangleUp(this,"slLongClose"+Low[0].ToString(), true, 0,Low[0] - TickSize, Brushes.DarkMagenta);
						ExitLong();
					}
                    break;
                case MarketPosition.Short:
					
					// If the channel is faling then raise the profit target higher
					if(IsFalling( HLSeries) 
						|| IsFalling( HMA(HMA_length))
					  )
					{
						SetProfitTarget(CalculationMode.Price, LL - trailProfitTrigger * TickSize );
						
					}
                    // Once the price is Less than entry price - breakEvenTicks ticks, set stop loss to breakeven
                    if (Close[0] < Position.AveragePrice - breakEvenTicks * TickSize
						&& previousPrice == 0)
                    {
						initialBreakEven = Position.AveragePrice - plusBreakEven * TickSize;
                       // SetStopLoss(CalculationMode.Price, initialBreakEven);
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
						//previousPrice = newPrice;
						Print("previousPrice = "+previousPrice);
					}
					
					//Short entry will close when price (Close) goes over LF
					if(CrossBelow(Close , LF,1) 
						|| IsRising( HLSeries)
						|| IsRising( HMA(HMA_length))
					    )
					{
						closeShortCondition = true ;
						Draw.Diamond(this,"CloseShortEntry"+Low[0].ToString(), true, 0,Low[0] - TickSize, Brushes.Green);
						ExitShort();
					}
					//Stop Loss Short entry will happen when price (Close) goes over HL
					if(CrossAbove(Close , stopLossShort,1))
					{
						slShortClose = true ;
						Draw.TriangleDown(this,"SL_Short"+Low[0].ToString(), true, 0,High[0] + TickSize, Brushes.Red);
						ExitShort();
					}
				
				
                    break;
                default:
					//autoStopA900Traded = false;
                    break;
			}

				
			}
			if (BarsInProgress == 1) // This is for 180 min data stored in index 1
			{
				
				//HL = Highs[1][0]; // MAX(Highs[1],1)[0];
				//LL = Lows[1][0];
				//Print("BB The Highest Value is "+ HL.ToString() + " Low = "+ LL.ToString());
				
				//Values[0][0] = HL ;
				//Values[1][0] = LL ;
			}

			
			if (BarsInProgress == 2) // This is for 5 min data stored in index 2
			{
				;
			}
			
		}

 

		[Browsable(false)]
		[XmlIgnore]
		public Series<double>HL_Plot
		{
			get {return Values[0];}
		}

		public Series<double>LL_Plot
		{
			get {return Values[1];}
		}
		
		#region Properties
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Fibonacci_Length", Order=1, GroupName="Parameters")]
		public int Fibonacci_Length
		{ get; set; }
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Higher TF in minutes", Order=1, GroupName="Parameters")]
		public int higherTF
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name="UseHMA", Order=2, GroupName="Parameters")]
		public bool UseHMA
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="HMA_length", Order=3, GroupName="Parameters")]
		public int HMA_length
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name="EnableTimeOfDay", Order=4, GroupName="Parameters")]
		public bool EnableTimeOfDay
		{ get; set; }

		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="StartTime", Order=5, GroupName="Parameters")]
		public DateTime StartTime
		{ get; set; }

		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="EndTime", Order=6, GroupName="Parameters")]
		public DateTime EndTime
		{ get; set; }

	
		[Description("Number of ticks past breakeven to actually breakeven")]
		[Category("Parameters")]
		public int PlusBreakEven
		{
			get { return plusBreakEven; }
			set { plusBreakEven = Math.Max(0, value); }
		}		
		
		[Description("Numbers of ticks away from entry price for the Stop Loss order")]
		[Category("Parameters")]
		public int StopLossTicks
		{
			get { return stopLossTicks; }
			set { stopLossTicks = Math.Max(0, value); }
		}
		
		[Description("Number of ticks away from entry price for the Profit Target order")]
		[Category("Parameters")]
		public int ProfitTargetTicks
		{
			get { return profitTargetTicks; }
			set { profitTargetTicks = Math.Max(0, value); }
		}
		
		[Description("Number of ticks away from break even")]
		[Category("Parameters")]
		public int BreakEvenTicks
		{
			get {return breakEvenTicks;}
			set {breakEvenTicks = Math.Max(0, value);}
		}
		
		[Description("Number of ticks to step for trail")]
		[Category("Parameters")]
		public int TrailStepTicks
		{
			get {return trailStepTicks;}
			set {trailStepTicks = Math.Max(0, value);}
		}
		
		[Description("Number of ticks to trigger trail step")]
		[Category("Parameters")]
		public int TrailProfitTrigger
		{
			get {return trailProfitTrigger;}
			set {trailProfitTrigger = Math.Max(0, value);}
		}

		
		#endregion

	}
}
