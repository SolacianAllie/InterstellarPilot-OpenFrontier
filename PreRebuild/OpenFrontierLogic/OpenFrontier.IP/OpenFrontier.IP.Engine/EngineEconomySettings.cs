using UnityEngine;
using UnityEngine.Serialization;

namespace OpenFrontier.IP.Engine
{
	/// <summary>
	/// All the knobs that decide how the in-game economy behaves: what things
	/// cost, what fractions are taxed, and how deliberately fuzzy the AI traders
	/// are about both. Sits under GameSettings.EconomySettings.
	/// </summary>
	public class EngineEconomySettings : MonoBehaviour
	{
		[Tooltip("The least trade efficiency a newly spawned faction AI can have. Each faction's efficiency is picked across the range from here up to 1.0, so this sets how bad the worst faction's trading can be.")]
		public float FactionAIMinTradeEfficiency = 0.2f;

		[Tooltip("Exponent applied to a faction's trade efficiency when sizing how large a price error the AI tolerates. Above 1 makes good factions disproportionately accurate and bad ones disproportionately terrible.")]
		public float FactionAITradeEfficiencyPower = 0.5f;

		[Tooltip("Tax fraction stations take on miscellaneous transactions. Applied at the point of sale, so it shows up in what the buyer pays rather than being a hidden fee.")]
		public float StationMiscTaxRate = 0.1f;

		[Tooltip("Tax fraction a station takes specifically when the player sells a ship to it. Kept much lower than the misc rate because a sale is already a poor price.")]
		public float StationShipSaleTaxRate = 0.025f;

		[Tooltip("Credits that ship sale prices are rounded up to. Keeps sold hulls on tidy numbers instead of quoting 14,317 credits. Applied on the buyer's final price, after tax and markup.")]
		public int ShipSaleRounding = 250;

		[Tooltip("Credits that all other AI-to-AI prices are rounded up to. The general case behind ShipSaleRounding, which uses a coarser step because hull prices are larger.")]
		public int GeneralRounding = 25;

		[Tooltip("Scales the price of every equipment component. One component class field to change rather than every unit class in the game.")]
		public float EquipmentCostMultiplier = 0.87f;

		[Tooltip("Lowest mining power a mineral deposit can be assigned, relative to its class default. The class value is picked randomly across this range, so deposits in the same field vary in richness.")]
		public float MiningMinPower = 0.6f;

		[Tooltip("Highest mining power a mineral deposit can be assigned, relative to its class default. Pairs with MiningMinPower.")]
		public float MiningMaxPower = 1.7f;

		[Tooltip("Fraction of a cargo factory's bay capacity deliberately left free for input goods, so the factory cannot fill the bay entirely with its own output.")]
		public float CargoFactoryBayInputReserve = 0.3f;

		[Tooltip("Fraction of a cargo factory's bay capacity allowed to hold finished output. The factory scales production to keep output within this share of the bay.")]
		public float CargoFactoryBayOutputReserve = 0.8f;

		[Tooltip("Score penalty per metre travelled when an AI fleet weighs a trade route. Negative, so distance always makes a route worse. Raising the magnitude makes the AI care much more about shipping distance.")]
		public float AITradeSearchScorePerMetreTravelled = -0.5f;

		[Tooltip("Least cost error the AI will tolerate when judging a trade. Low end of a random range; pairs with AITradeEfficiencyMaxCostFuzziness.")]
		public float AITradeEfficiencyMinCostFuzziness = 0.2f;

		[Tooltip("Most cost error the AI will tolerate when judging a trade. High end of that same random range, so the gap between these two is the spread in how wrong the AI's cost estimate can be.")]
		public float AITradeEfficiencyMaxCostFuzziness = 1f;

		[Tooltip("How wrong the AI's belief about a cargo's price gets when the buying faction is very good at trade. The believed price is interpolated between the cargo's real base price and a flat 100 credits; at full efficiency this is the end it leans toward.")]
		public float AITradeSearchMinPriceObscure = 0.2f;

		[Tooltip("How wrong the AI's belief about a cargo's price gets when the buying faction is very bad at trade. The other end of the same interpolation, so the gap between the two is the total span of AI price blindness.")]
		public float AITradeSearchMaxPriceObscure = 0.8f;

		[Tooltip("Penalty applied to a trade's score when the buyer's hold is nearly full, as a fraction of the score. At 0.5 a completely full hold costs half the trade's appeal.")]
		public float AITradeSearchHighStockLevelCostDiscount = 0.5f;

		[Tooltip("Cargo volume, in a buyer's own units, that counts as a completely full hold for the penalty above. The fullness is measured as a fraction of this and clamped to it, so it is the denominator that decides how fast that penalty ramps in.")]
		public float AITradeSearchHighStockReferenceValue = 5f;

		[Tooltip("Penalty applied to a trade's score when the seller's stock is running low. The mirror of AITradeSearchHighStockLevelCostDiscount, on the selling side.")]
		public float AITradeSearchLowStockLevelCostDiscount = 0.5f;

		[Tooltip("Stock level below which a seller counts as running low and starts paying the penalty above. A fraction of the factory's maximum stock.")]
		public float AITradeSearchLowStockLevelThreshold = 0.4f;

		[Tooltip("Stock level above which a seller counts as sitting on a surplus. A fraction of the factory's maximum stock, and the trigger for the high-stock discount on the buyer's side.")]
		public float AITradeSearchHighStockLevelThreshold = 0.6f;

		[Tooltip("Seconds between clearing a trader's cached price list. Prices are cached so repeated haggling does not re-cost the whole sector, so this is how stale a quote can get before it is recomputed.")]
		public float ClearTraderCachedPricesInterval = 10f;

		[Tooltip("Lets a faction's opinion of you move the price it quotes. Off means relations never affect prices and only goods and greed matter.")]
		public bool ApplyGoodRelationsMarkup = true;

		[Tooltip("Lets a station's own markup apply to the prices it quotes. Off makes stations quote closer to the base price, with only opinion and tax moving the number.")]
		public bool ApplyTraderMarkup = true;

		[Tooltip("Scales how much ore an asteroid yields. Applied to the random roll between the asteroid class's min and max yield, so this is the one multiplier that sets asteroid richness game-wide.")]
		public float AsteroidYieldQuantityMultiplier = 3f;

		[Tooltip("Top of the multiplier band a factory buys within, used when its stock has fallen below LowStockMin and it needs the goods. It bids up its own scarcity here, paying more than CargoFactoryMinBuyPrice.")]
		public float CargoFactoryMaxBuyPrice = 1.3f;

		[Tooltip("Top of the multiplier band a factory sells within, used when it is desperate to shift stock. Despite the 'Max' it is still under base price - a factory never sells above cost. The bottom of the band is CargoFactoryMinSellPrice.")]
		public float CargoFactoryMaxSellPrice = 0.9f;

		[Tooltip("Low end of the multiplier band a factory pays within, used when it has stock to spare and can afford to be patient. When it runs short it pays up to CargoFactoryMaxBuyPrice instead.")]
		public float CargoFactoryMinBuyPrice = 1.1f;

		[Tooltip("Low end of the multiplier band a factory sells within, used when it has surplus above its high-stock level and just wants the hold cleared. The top of the band is CargoFactoryMaxSellPrice.")]
		public float CargoFactoryMinSellPrice = 0.7f;

		[Tooltip("Stock level treated as high, as a fraction of the factory's maximum. Above this the factory seeks buyers rather than holding.")]
		public float CargoFactoryHighStock = 0.75f;

		[Tooltip("Stock level treated as low, as a fraction of the factory's maximum. Below this the factory looks to buy.")]
		public float CargoFactoryLowStock = 0.4f;

		[Tooltip("Absolute floor on the low-stock level, as a fraction of the factory's maximum stock. Stops a factory with a tiny bay from treating even one unit as running out.")]
		public float CargoFactoryLowStockMin = 0.25f;

		[Tooltip("Scales the input goods a cargo factory reserves when working out what it can produce, so it keeps a larger buffer of raw material relative to its bay.")]
		public float CargoFactoryRequiredInputsMultiplier = 0.75f;

		[Tooltip("How much a faction's opinion shifts the price it quotes for cargo. Zero means opinion is ignored entirely; larger values make a disliked faction's prices move much further from base.")]
		public float CargoGoodOpinionMultiplier = 0.2f;

		[Tooltip("Exponent on faction opinion when shifting prices. Above 1 makes opinion bite hard at the extremes while barely moving near neutral, which is what a curve on a small multiplier needs.")]
		public float CargoGoodOpinionPower = 2f;

		[Tooltip("How strongly the price colour in the trade UI reacts to a price moving away from base. Larger values need a bigger price deviation before the colour shifts at all, so the bar stays readable.")]
		public float CargoPriceMultiplierColorAffect = 0.5f;

		[Tooltip("Revenue credited per unit of cargo a consumer faction actually delivers. Its base price times this times quantity is the money that actually changes hands, separate from anything the seller hoped for.")]
		public float ConsumerRevenueMultiplier = 1.7f;

		[Tooltip("Scales how long consumers take to convert inputs into finished goods. Larger values make the economy slower and goods scarcer.")]
		public float ConsumerProductionTimeMultiplier = 0.5f;

		[Tooltip("Preferred length of one production cycle in seconds at full volume. Shorter cycles mean a steadier supply of goods and less dramatic swings between surplus and shortage.")]
		public float PreferredProductionTimeCycle = 60f;

		[Tooltip("Seconds of production time per credit of input cost. Production time is worked out from what the job costs to run, so this is what converts a price into a duration.")]
		public float ProductionTimePerCredits = 0.01f;

		[Tooltip("Lets a price setter assign prices to the whole goods list in one go, rather than only the specific goods it names. Off means price setters can only touch goods they list explicitly.")]
		public bool SetMiscPrices;

		[Tooltip("Largest fraction of a trader's hold it can spawn already filled. Picked randomly within the range down to TraderInitWithCargoMinVolume, so traders start part-loaded rather than empty or full.")]
		public float TraderInitWithCargoMaxVolume = 0.6f;

		[Tooltip("Smallest fraction of a trader's hold it can spawn filled, paired with TraderInitWithCargoMaxVolume.")]
		public float TraderInitWithCargoMinVolume = 0.3f;

		[Tooltip("Chance a newly spawned trader starts carrying cargo at all rather than empty, 0-1.")]
		public float TraderInitWithCargoProbability = 0.75f;

		[FormerlySerializedAs("TraderMarkup")]
		[Tooltip("Markup a maximally greedy faction charges over base price. The faction's greed picks a point across the range from TraderMarkupLower up to here.")]
		public float TraderMarkupUpper = 0.2f;

		[Tooltip("Markup a completely selfless faction charges over base price. The other end of the same greed-driven range.")]
		public float TraderMarkupLower = 0.2f;

		[Tooltip("Lets AI traders unload cargo they cannot sell, so they do not sit permanently full and stop buying.")]
		public bool TrimTraderCargo = true;

		[Tooltip("How full a trader's hold has to be before cargo gets trimmed. Above this the trader dumps stock it cannot move.")]
		public float TrimTraderCargoThresholdCargoUsage = 0.85f;

		[Tooltip("Fraction of a cargo type removed when a trader's hold gets trimmed. 0.5 halves it in one pass, which is usually enough to get back under the threshold.")]
		public float TrimTraderCargoTrimLevel = 0.5f;

		[Tooltip("Credits to repair one point of hull damage. Repair cost is this times the damage, so it is the main price control on field repairs.")]
		public float RepairCostPerHullPoint = 0.01f;

		[Tooltip("Credits one point of hull is worth. Used when a hull is dismantled or sold on, so it sets what a damaged hull still fetches.")]
		public float HullValuePerHullPoint = 20f;

		[Tooltip("Virtue below which a faction counts cargo as illegal and will not trade it. Negative, because low virtue is bad. Reputations above the threshold trade freely.")]
		public float TradeIllegalCargoThreshold = -0.1f;

		[Tooltip("Penalty applied to a trade's score when the buyer and seller are at the same station, as a fraction of the score. Stops AI from endlessly shuffling goods to its own docks.")]
		public float OwnStationTradeScorePercentage = 0.15f;
	}
}
