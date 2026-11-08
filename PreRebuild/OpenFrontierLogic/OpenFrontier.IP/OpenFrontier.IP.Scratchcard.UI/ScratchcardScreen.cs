using System;
using System.Collections.Generic;
using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Scratchcard.Model;
using OpenFrontier.IP.UI;
using UnityEngine;
using UnityEngine.UI;
using Random = System.Random;

namespace OpenFrontier.IP.Scratchcard.UI
{
	public class ScratchcardScreen : EngineScreen
	{
		public GameObject ScratchcardPanelRoot;

		public GameObject ScratchcardRoot;

		public Faction Faction;

		public ScratchcardDrawer ScratchcardDrawer;

		public ScratchcardGenerator ScratchcardGenerator;

		public ScratchcardSetup ScratchcardSetup;

		public ScratchcardPrizeItem PrizeItemPrefab;

		public GameObject PrizeItemsRoot;

		public int SlotCount = 9;

		public int MatchingSlotsToWin = 3;

		public Text ResultLabel;

		public ScratchcardOverlay Overlay;

		public float RequiredScratchAmount = 0.8f;

		private ScratchcardPrize currentPrizeResult;

		private List<ScratchcardPrizeItem> prizeItems = new List<ScratchcardPrizeItem>();

		public Button BuyTicketButton;

		public SineRotator ScratchcardRotator;

		public float DelayBeforeRestartAfterGame = 4f;

		private float timeOfGameEnd;

		protected override void awake()
		{
			base.awake();
			Reset();
		}

		public void GenerateScratchcard()
		{
			GeneratePrizeResult(out var prizeResult, out var prizes);
			CreateScratchcardPrizes(prizeResult, prizes);
		}

		public void Play()
		{
			GenerateScratchcard();
			Reset();
			DeductStake();
			Overlay.enabled = true;
			BuyTicketButton.gameObject.SetActive(value: false);
			PrizeItemsRoot.gameObject.SetActive(value: true);
			ScratchcardRotator.Reset();
			ScratchcardRotator.gameObject.SetActive(value: false);
		}

		private void GeneratePrizeResult(out ScratchcardPrize prizeResult, out ScratchcardPrize[] prizes)
		{
			System.Random random = new System.Random();
			prizeResult = ScratchcardDrawer.Draw(ScratchcardSetup, random);
			prizes = ScratchcardGenerator.Generate(random, prizeResult, ScratchcardSetup.Prizes, SlotCount, MatchingSlotsToWin);
			currentPrizeResult = prizeResult;
		}

		private void Reset()
		{
			PrizeItemsRoot.gameObject.SetActive(value: false);
			PrepareOverlay();
			ScratchcardRotator.gameObject.SetActive(value: true);
			ResultLabel.gameObject.SetActive(value: false);
			Overlay.enabled = false;
		}

		private void PrepareOverlay()
		{
			Overlay.gameObject.SetActive(value: true);
			Overlay.Reset();
		}

		protected override void update()
		{
			base.update();
			if (currentPrizeResult != null)
			{
				if (Overlay.Progress > RequiredScratchAmount)
				{
					OnScratchcardRubbed();
				}
			}
			else if (PrizeItemsRoot.activeSelf && RealTime.time - timeOfGameEnd > DelayBeforeRestartAfterGame)
			{
				Reset();
			}
		}

		private void OnScratchcardRubbed()
		{
			timeOfGameEnd = RealTime.time;
			RecordStats();
			ApplyWinnings(currentPrizeResult);
			PopulateResultText(currentPrizeResult);
			ResultLabel.gameObject.SetActive(value: true);
			HighlightWinIfNeeded();
			currentPrizeResult = null;
			BuyTicketButton.gameObject.SetActive(value: true);
		}

		private void RecordStats()
		{
			if (Faction != null && Faction.Stats != null)
			{
				Faction.Stats.HighestScratchcardWin = Mathf.Max(Faction.Stats.HighestScratchcardWin, currentPrizeResult.PrizeValue);
				Faction.Stats.ScratchcardsScratched++;
			}
		}

		private void HighlightWinIfNeeded()
		{
			if (!currentPrizeResult.IsWinner)
			{
				return;
			}
			foreach (ScratchcardPrizeItem prizeItem in prizeItems)
			{
				if (prizeItem.Prize == currentPrizeResult)
				{
					prizeItem.HighlightWin();
				}
			}
		}

		private void PopulateResultText(ScratchcardPrize prize)
		{
			ResultLabel.text = GetResultText(prize);
		}

		private string GetResultText(ScratchcardPrize prize)
		{
			if (prize.IsWinner)
			{
				return $"Congratulations, you won {prize.PrizeValue:N0} credits!";
			}
			return "You did not win this time... better luck next time!";
		}

		private void CreateScratchcardPrizes(ScratchcardPrize winningPrize, ScratchcardPrize[] prizes)
		{
			prizeItems.Clear();
			UnityObjectHelper.DestroyChildren(PrizeItemsRoot, destroyImmediate: true);
			foreach (ScratchcardPrize prize in prizes)
			{
				ScratchcardPrizeItem scratchcardPrizeItem = UnityObjectHelper.InstantiateAndGetComponent(PrizeItemPrefab);
				scratchcardPrizeItem.Prize = prize;
				scratchcardPrizeItem.Refresh();
				scratchcardPrizeItem.transform.SetParent(PrizeItemsRoot.transform, worldPositionStays: true);
				scratchcardPrizeItem.transform.localScale = Vector3.one;
				scratchcardPrizeItem.transform.localRotation = Quaternion.identity;
				prizeItems.Add(scratchcardPrizeItem);
			}
		}

		private void DeductStake()
		{
			Eng.AddCreditsToPlayerFactionWithMsg(-ScratchcardSetup.CostPerCard, FactionTransactionType.Scratchcard, null, Eng.PlayerRootUnit);
		}

		private void ApplyWinnings(ScratchcardPrize winningPrize)
		{
			if (winningPrize.IsWinner)
			{
				Eng.AddCreditsToPlayerFactionWithMsg(winningPrize.PrizeValue, FactionTransactionType.Scratchcard, null, Eng.PlayerRootUnit);
			}
		}
	}
}
