#define PROFILE


using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;

/**
 * Auto-generated code below aims at helping you parse
 * the standard input according to the problem statement.
 **/
//TODO: Niektore spelle moga miec inne targety a dawac ten sam wynik score'a
//TODO: sortuj wyniki summon i cast. symuluj ataki jesli liczba atakujacych sie nie zmienia to przepisz ataki (z execute)
//TODO: zapisywac wartosci ataku, obrony i abilitki z przed rzucenia zielonego czaru i sprawdzac podczas execute czy cos to dalo
//TODO: przeciwnik ma L ja mam G. moj minion atakuje L mimo ze L i tak musial by sie poswiecic w turze przeciwnika  https://www.codingame.com/replay/333959666
class MainClass
{
	static void Main (string[] args)
	{
		Game game = new Game ();
		game.Execute ();
	}
}

class Game
{
	private const int GENERATING_TIMEOUT_LIMIT = 92;
	private const int SCORING_COMBINATIONS_PER_MS = 750;
	private const int SCORING_TIMEOUT_LIMIT = 96;
	private const int BOARD_LIMIT = 6;
	private const int PICK_CURVE_STABILIZATION_THRESHOLD = 8;
	private const int CARDS_IN_DECK = 30;
	DataContainer dataContainer;
	DateTime turnStartTime;
	public List<Card> pickedCards = new List<Card> ();
	static int mySimulations = 0;
	static int enemySimulations = 0;
	static int discardedSimulations = 0;


	private float GetGeneratingTimeoutLimit (List<CardCombination> combinations)
	{
		return (float)GENERATING_TIMEOUT_LIMIT - (combinations.Count / SCORING_COMBINATIONS_PER_MS);
	}

	public void Execute ()
	{
		Debug.enabled = true;
		while (true)
		{
			string actionToPerform = GenerateTurn ();
			Debug.PrintProfiler ();
			Debug.ClearProfiler ();
			Console.WriteLine (actionToPerform);
		}
	}

	private string GenerateTurn ()
	{
		mySimulations = 0;
		enemySimulations = 0;
		dataContainer = new DataContainer ();
		dataContainer.me = new Player (Console.ReadLine ());
		dataContainer.enemy = new Player (Console.ReadLine ());

		int opponentHand = int.Parse (Console.ReadLine ());
		int cardCount = int.Parse (Console.ReadLine ());
		dataContainer.Clear ();

		for (int i = 0; i < cardCount; i++)
		{
			Card newC = new Card (Console.ReadLine ());
			if (i == 0)
			{
				turnStartTime = DateTime.Now;
			}

			if (newC.location != (int)Card.CardLocation.Enemy)
			{
				newC.player = dataContainer.me;
				dataContainer.myCards.Add (newC);
			} else
			{

				newC.player = dataContainer.enemy;
				dataContainer.enemyCards.Add (newC);
			}
		}

		if (IsCardPick ())
		{
			PickBestCard ();
		} else
		{
			Play ();
		}
		if (dataContainer.gameActions.Count == 0)
		{
			dataContainer.AddAction (GameAction.ActionType.PASS, null, null);
		}
		string actionToPerform = "";
		string msgToAppend = (DateTime.Now - turnStartTime).TotalMilliseconds.ToString ("F1") + " - " + mySimulations + "/" + enemySimulations + "/" + discardedSimulations;

		for (int i = 0; i < dataContainer.gameActions.Count; i++)
		{
			actionToPerform += dataContainer.gameActions[i].ToString () + " " + msgToAppend;

			if (i < dataContainer.gameActions.Count - 1)
			{
				actionToPerform += ";";
			}
		}
		Debug.Log ("TurnEndTime " + (DateTime.Now - turnStartTime).TotalMilliseconds + "  in miliseconds");
		return actionToPerform;
	}

	bool ConciderAttackingMinions ()
	{
		return dataContainer.me.startMana == dataContainer.enemy.mana;
	}

	bool IsCardPick ()
	{
		return dataContainer.me.mana + dataContainer.enemy.mana == 0;
	}

	void PickBestCard ()
	{
		Card best = GetBestCard (dataContainer.myCards);
		dataContainer.AddAction (GameAction.ActionType.PICK, dataContainer.myCards.IndexOf (best));
		pickedCards.Add (best);
	}

	void Play ()
	{
		List<CardCombination> availableCombinations = GenerateCombinations ();
		CardCombination bestCombo = GetBestCombinations (availableCombinations, 1)[0];

		dataContainer = bestCombo.dataContainer;
		//ManageIdleAttackers (bestCombo);
	}

	private List<CardCombination> GetBestCombinations (List<CardCombination> availableCombinations, int count, bool enemyTurn = false)
	{
		int bestIndex = 0;
		float bestScore = float.MinValue;
		List<CardCombination> cardCombinations = new List<CardCombination> (count);
		Dictionary<int, float> scores = new Dictionary<int, float> ();
		bool thisTurn = false;
		for (int i = 0; i < availableCombinations.Count; i++)
		{

			//bool containsAttacks = false;// (from n in availableCombinations[i].dataContainer.gameActions where n.actionType == GameAction.ActionType.ATTACK select n).ToList().Count > 2;
			//	containsAttacks = (from n in availableCombinations[i].dataContainer.gameActions where n.actonExecuter == 2  && n.actonTarget == 23 select n).ToList ().Count > 0; 


			//if (containsAttacks)
			//{
			//	thisTurn = true;
			//Debug.Log ("---------------------------------------------------");

			//Debug.Log ("Score " + availableCombinations[i].dataContainer.id);
			//	//string myCr = "My Creatures: ";
			//	//for (int j = 0; j < availableCombinations[i].dataContainer.myCards.Count; j++)
			//	//{
			//	//	if (availableCombinations[i].dataContainer.myCards[j].location == (int)Card.CardLocation.Board)
			//	//	{
			//	//		myCr += availableCombinations[i].dataContainer.myCards[j].instanceId + " a:" + availableCombinations[i].dataContainer.myCards[j].attack + " d:" + availableCombinations[i].dataContainer.myCards[j].defense + "; ";
			//	//	}
			//	//}
			//	//Debug.Log (myCr);

			//	//myCr = "Enemyy Creatures: ";
			//	//for (int j = 0; j < availableCombinations[i].dataContainer.enemyCards.Count; j++)
			//	//{
			//	//	if (availableCombinations[i].dataContainer.enemyCards[j].location == (int)Card.CardLocation.Enemy)
			//	//	{
			//	//		myCr += availableCombinations[i].dataContainer.enemyCards[j].instanceId + " a:" + availableCombinations[i].dataContainer.enemyCards[j].attack + " d:" + availableCombinations[i].dataContainer.enemyCards[j].defense + "; ";
			//	//	}
			//	//}
			//	//Debug.Log (myCr);
			//availableCombinations[i].PrintActions ();
			//	}

			float score = enemyTurn ? ScorePostEnemyAttack (availableCombinations[i]) : ScoreCombination (availableCombinations[i]);
			//if (containsAttacks)
			//{
			//Debug.Log ("			Combo scored " + availableCombinations[i].dataContainer.id + " with score " + score + "  offset:" + availableCombinations[i].scoreOffset);
			//Debug.Log ("-------------------------------------------------- -");
			//}
			scores.Add (i, score);
			if (score > bestScore)
			{
				bestScore = score;
				bestIndex = i;

			}
			if ((DateTime.Now - turnStartTime).TotalMilliseconds > SCORING_TIMEOUT_LIMIT)
			{
				Debug.Log ("scoring EMERGENCY BREAK " + (DateTime.Now - turnStartTime).TotalMilliseconds +" scored:"+i+ "/" + availableCombinations.Count);

				break;
			}
		}
		//if (thisTurn)
		//{
		//	Debug.Log ("-------------------------------------------------- -");

		//Debug.Log ("Score " + availableCombinations[bestIndex].dataContainer.id + "    bestScore " + bestScore);
		//availableCombinations[bestIndex].PrintActions ();

		//string myCr = "My Creatures: ";
		//for (int j = 0; j < availableCombinations[bestIndex].dataContainer.myCards.Count; j++)
		//{
		//	if (availableCombinations[bestIndex].dataContainer.myCards[j].location == (int)Card.CardLocation.Board)
		//	{
		//		myCr += availableCombinations[bestIndex].dataContainer.myCards[j].instanceId + " a:" + availableCombinations[bestIndex].dataContainer.myCards[j].attack + " d:" + availableCombinations[bestIndex].dataContainer.myCards[j].defense + "; ";
		//	}
		//}
		//Debug.Log (myCr);

		//myCr = "Enemyy Creatures: ";
		//for (int j = 0; j < availableCombinations[bestIndex].dataContainer.enemyCards.Count; j++)
		//{
		//	if (availableCombinations[bestIndex].dataContainer.enemyCards[j].location == (int)Card.CardLocation.Enemy)
		//	{
		//		myCr += availableCombinations[bestIndex].dataContainer.enemyCards[j].instanceId + " a:" + availableCombinations[bestIndex].dataContainer.enemyCards[j].attack + " d:" + availableCombinations[bestIndex].dataContainer.enemyCards[j].defense + "; ";
		//	}
		//}
		//Debug.Log (myCr);
		//Debug.Log ("			Combo scored " + availableCombinations[bestIndex].dataContainer.id + " with score " + bestScore + "  offset:" + availableCombinations[bestIndex].scoreOffset);
		//Debug.Log ("-------------------------------------------------- -");
		//}

		List<KeyValuePair<int, float>> sorted = null;
		if (enemyTurn)
		{
			sorted = scores.OrderBy (n => n.Value).ToList ();
		} else
		{
			sorted = scores.OrderByDescending (n => n.Value).ToList ();
		}

		for (int i = 0; i < sorted.Count && i < count; i++)
		{
			cardCombinations.Add (availableCombinations[sorted[i].Key]);

		}
		return cardCombinations;
	}

	private void ManageIdleAttackers (CardCombination cardCombination)
	{
		if (!DoesEnemyHaveGuard (cardCombination.dataContainer.enemyCards))
		{
			List<Card> allAttackers = new List<Card> ();
			GetAttackers (allAttackers, cardCombination, cardCombination.dataContainer.myCards);
			for (int i = 0; i < allAttackers.Count; i++)
			{
				cardCombination.dataContainer.AddAction (GameAction.ActionType.ATTACK, allAttackers[i].instanceId, -1);
			}
		}
	}

	public float ScorePostEnemyAttack (CardCombination cardCombination)
	{
		float score = 0;
		if (cardCombination.dataContainer.me.health <= 0)
		{
			score = float.MinValue / 2;
		}
		score += cardCombination.dataContainer.me.health / 2f;
		//IS: podsumowanie mojej planszy
		int creaturesAttack = 0;
		int creaturesHealth = 0;
		float creaturesScore = 0;

		for (int i = 0; i < cardCombination.dataContainer.myCards.Count; i++)
		{
			if (cardCombination.dataContainer.myCards[i].location == (int)Card.CardLocation.Board)
			{
				creaturesAttack += cardCombination.dataContainer.myCards[i].attack;
				creaturesHealth += cardCombination.dataContainer.myCards[i].defense;
				creaturesScore += ScoreCreatureOnBoardComboStyle (cardCombination.dataContainer.myCards[i]) + 1;
			}
		}


		score += ((creaturesAttack * 2) + creaturesHealth + creaturesScore) * 2;

		creaturesAttack = 0;
		creaturesHealth = 0;
		creaturesScore = 0;

		for (int i = 0; i < cardCombination.dataContainer.enemyCards.Count; i++)
		{
			creaturesAttack += cardCombination.dataContainer.enemyCards[i].attack;
			creaturesHealth += cardCombination.dataContainer.enemyCards[i].defense;
			creaturesScore += ScoreCreatureOnBoardComboStyle (cardCombination.dataContainer.enemyCards[i]);
		}

		score -= ((creaturesAttack * 2) + creaturesHealth + creaturesScore) * 2;
		return score;
	}

	private float ScoreCombination (CardCombination cardCombination)
	{
		float score = 0;
		if (cardCombination.dataContainer.enemy.health <= 0)
		{
			score = float.MaxValue / 2;
			return score;
		}
		if (cardCombination.dataContainer.me.health <= 0)
		{
			score = float.MinValue / 2;
		}
		//IS: podsumowanie mnie i przeciwnika
		score += (30 - cardCombination.dataContainer.enemy.health) * 2f;
		score += (cardCombination.dataContainer.me.health / 3);

		//Debug.Log ("Health: "+ score);
		int cardsInHand = 0;

		//IS: podsumowanie mojej planszy
		int allMyCreaturesAttack = 0;
		int allMyCreaturesHealth = 0;

		float allMyCreaturesScore = 0;

		int myHandCreaturesAttack = 0;
		int myHandCreaturesHealth = 0;

		float myHandCreaturesScore = 0;

		int myStrongestAttack = 0;
		int myStrongestDefence = 0;
		int myGuardsHealth = 0;
		int myWardedGuards = 0;
		int myGuardsOnBoard = 0;

		for (int i = 0; i < cardCombination.dataContainer.myCards.Count; i++)
		{
			Card card = cardCombination.dataContainer.myCards[i];
			if (card.location == (int)Card.CardLocation.Board || card.location == (int)Card.CardLocation.Hand && card.cardType != (int)Card.CardType.Creature)
			{
				allMyCreaturesAttack += Math.Min (Math.Abs (card.attack), card.cardType == (int)Card.CardType.RedItem ? Math.Max (card.cost, 2) : 12);
				allMyCreaturesHealth += Math.Min (Math.Abs (card.defense), card.cardType == (int)Card.CardType.RedItem ? Math.Max (card.cost, 2) : 12);
				allMyCreaturesScore += ScoreCreatureOnBoardComboStyle (card) + 1;

				if (card.IsLethal())
				{
					allMyCreaturesScore += 5;
				}
				//Debug.Log (" = " + cardCombination.dataContainer.myCards[i].instanceId + " a:" + Math.Abs (cardCombination.dataContainer.myCards[i].attack) + " d:" + Math.Abs (cardCombination.dataContainer.myCards[i].defense) + " s:" + (ScoreCreatureOnBoardComboStyle (cardCombination.dataContainer.myCards[i]) + 1));

				if (card.location == (int)Card.CardLocation.Hand)
				{
					cardsInHand++;
					if (cardCombination.dataContainer.me.mana >= card.cost)
					{
						cardCombination.dataContainer.me.mana = Math.Max (0, cardCombination.dataContainer.me.mana - card.cost);
					}
				} else
				{
					if (card.IsGuard ())
					{
						myGuardsOnBoard++;
						myGuardsHealth += card.defense;
						if (card.IsWard())
						{
							myWardedGuards++;
						}
					}
				}
			} else
			{
				cardsInHand++;
				myHandCreaturesAttack += Math.Min (Math.Abs (card.attack), card.cardType == (int)Card.CardType.RedItem ? Math.Max(card.cost ,2): 12);
				myHandCreaturesHealth += Math.Min (Math.Abs (card.defense), card.cardType == (int)Card.CardType.RedItem ? Math.Max (card.cost, 2) : 12);
				myHandCreaturesScore += ScoreCreatureOnBoardComboStyle (card);
			}
			if (card.attack > myStrongestAttack)
			{
				myStrongestAttack = card.attack;
			}
			if (card.defense > myStrongestDefence)
			{
				myStrongestDefence = card.defense;
			}

		}

		int myCreaturesOnBoard = cardCombination.dataContainer.myCards.Count - cardsInHand;
		score += myCreaturesOnBoard * 3;
		score += (myCreaturesOnBoard - myGuardsOnBoard) * 2;

		score += ((allMyCreaturesAttack) + allMyCreaturesHealth) * 2;
		score += allMyCreaturesScore;
		//Debug.Log ("myCreaturesBoard: " + score);

		score += ((myHandCreaturesAttack + myHandCreaturesHealth) * 2) * 0.5f;
		score += myHandCreaturesScore * 0.4f;
		//score -= cardCombination.dataContainer.me.mana;
		//Debug.Log ("myCreaturesHand: " + score);
		score += Math.Min (8 - cardsInHand, cardCombination.dataContainer.drawCount) * 3;
		//Debug.Log ("mana: " + score);


		//IS: podsumowanie planszy przeciwnika
		int allEnemyCreaturesAttack = 0;
		int allEnemyCreaturesHealth = 0;
		float allEnemyCreaturesScore = 0;
		int smallestEnemyAttack = int.MaxValue;
		int enemyGuards = 0;


		for (int i = 0; i < cardCombination.dataContainer.enemyCards.Count; i++)
		{
			Card enemyCard = cardCombination.dataContainer.enemyCards[i];
			allEnemyCreaturesAttack += Math.Abs (enemyCard.attack);
			allEnemyCreaturesHealth += Math.Abs (enemyCard.defense);
			allEnemyCreaturesScore += ScoreCreatureOnBoardComboStyle (enemyCard);
			if (enemyCard.attack < smallestEnemyAttack)
			{
				smallestEnemyAttack = enemyCard.attack;
			}
			enemyGuards += enemyCard.IsGuard () ? 1 : 0;
		}

		score -= enemyGuards * 2;
		score -= cardCombination.dataContainer.enemyCards.Count * 4;
		score -= ((allEnemyCreaturesAttack * 4) + allEnemyCreaturesHealth);
		score -= allEnemyCreaturesScore * 3;

		//Debug.Log ("EnemyCreatures: " + score+"   "+(cardCombination.dataContainer.enemyCards.Count * 10)+"  "+(((allEnemyCreaturesAttack * 2) + allEnemyCreaturesHealth) * 2)+"   "+(allEnemyCreaturesScore * 3));

		if (allEnemyCreaturesAttack - (myGuardsHealth + (myWardedGuards * smallestEnemyAttack)) > cardCombination.dataContainer.me.health)
		{
			score -= 1000;
		}

		score += cardCombination.scoreOffset;
		//Debug.Log ("Rest: " + score);

		return score;
	}

	public static float ScoreCreatureOnBoardComboStyle (Card creature)
	{
		float score = 0;
		if (creature.cardType != (int)Card.CardType.RedItem)
		{
			score += creature.IsLethal () ? (float)creature.attack : 0;
			score += creature.IsWard () ? Math.Max((float)creature.attack / 2 ,1): 0;
			//score += creature.IsGuard () ? (float)creature.defense / 2 : 0;
			if (creature.location == (int)Card.CardLocation.Hand)
			{
			score += creature.IsCharge () ? Math.Max((float)((creature.IsLethal () ? creature.attack: 0 ) +creature.attack) ,1): 0;

			}
		}

		if (!creature.IsCreature ())
		{
			score += (float)creature.bAbilities > 0 ? 1 : 0;
			score += (float)creature.cost; //IS: zeby nie poswiecac na zbyt male stworki
		}
		return score;
	}

	public static int ScoreCreatureOnBoard (Card creature, bool countWAndL, bool countAsCreature = false)
	{
		int score = 0;
		if (countAsCreature || creature.IsCreature ())
		{
			score += creature.IsBreakthrough () ? creature.attack / 2 : 0;
			if (countWAndL)
			{
				score += creature.IsLethal () ? creature.attack + 10 : 0;
				score += creature.IsWard () ? creature.defense : 0;
			}
			score += creature.IsDrain () ? creature.attack : 0;
			score += creature.IsGuard () ? creature.defense / 2 : 0;
		} else
		{
			score += creature.bAbilities > 0 ? 1 : 0;
		}

		return score;
	}


	private void GenerateSummonCombination (CardCombination combination, List<Card> availableToPlay, int startIndex, List<CardCombination> combinations, DataContainer thisDataContainer)
	{
		if ((DateTime.Now - turnStartTime).TotalMilliseconds > GetGeneratingTimeoutLimit(combinations))
		{
			Debug.enabled = true;

			Debug.Log ("EMERGENCY BREAK GenerateSummonCombination " + (DateTime.Now - turnStartTime).TotalMilliseconds + "  totalCombos " + combinations.Count);
			throw new TimeOutException ();
		}

		if (GetMyCreaturesOnBoard (thisDataContainer) >= BOARD_LIMIT)
		{
			return;
		}
		int currentMana = thisDataContainer.me.mana;
		const GameAction.ActionType action = GameAction.ActionType.SUMMON;
		for (int i = startIndex; i < availableToPlay.Count; i++)
		{
			if (availableToPlay[i].cost <= currentMana)
			{
				CardCombination copy = MakeNewCombination (combination, availableToPlay[i], null, action);
				bool comboAdded = AddComboToList (combinations, copy);
				if (comboAdded)
				{
					copy.AddMana (availableToPlay[i], action);
					ExecuteAction (copy.dataContainer.lastAction, copy.dataContainer);

					GenerateSummonCombination (copy, availableToPlay, i + 1, combinations, copy.dataContainer);
				}
			}
		}
	}

	private static int GetMyCreaturesOnBoard (DataContainer thisDataContainer)
	{
		int onBoard = 0;
		for (int i = 0; i < thisDataContainer.myCards.Count; i++)
		{
			if (thisDataContainer.myCards[i].location == (int)Card.CardLocation.Board)
			{
				onBoard++;
			}
		}

		return onBoard;
	}

	private CardCombination MakeNewCombination (CardCombination parentCombination, Card parentCard, Card parentTarget, GameAction.ActionType action)
	{
		CardCombination copy = new CardCombination (parentCombination);
		GameAction gameAction = copy.dataContainer.AddAction (action, parentCard, parentTarget);
		return copy;
	}


	List<CardCombination> GenerateCombinations ()
	{
		List<Card> availableToPlay = new List<Card> (dataContainer.myCards.Count);
		GetCardsAvailableToSummon (availableToPlay);
		List<CardCombination> combinations = new List<CardCombination> (dataContainer.myCards.Count + (dataContainer.enemyCards.Count * (dataContainer.myCards.Count - availableToPlay.Count)));

		CardCombination emptyCombination = new CardCombination ();
		DataContainer newDataContainer = new DataContainer (dataContainer);
		emptyCombination.dataContainer = newDataContainer;
		combinations.Add (emptyCombination);

		List<CardCombination> enemyCombinations = new List<CardCombination> (2000);

		try
		{
			GenerateCombination_Summons (availableToPlay, combinations, emptyCombination);
			GenerateCombination_Spells (availableToPlay, combinations);


			combinations = GetBestCombinations (combinations, Math.Max (1, combinations.Count));

			GenerateCombination_GuardAttack (availableToPlay, combinations);

			combinations = GenereateCombination_MyAttacks (availableToPlay, combinations);
			combinations = GenerateCombination_EnemyAttacks (availableToPlay, combinations, enemyCombinations);

		} catch (TimeOutException)
		{
			Debug.enabled = true;
			enemySimulations += enemyCombinations.Count;

			Debug.Log ("caught exception "+ GetGeneratingTimeoutLimit(combinations));
		}
		mySimulations = combinations.Count;
		Debug.Log ("SIM COUNT. ME:" + mySimulations + "; ENEMY:" + enemySimulations + "; DISCARDED:" + discardedSimulations);
		return combinations;
	}

	private void GenerateCombination_GuardAttack (List<Card> availableToPlay, List<CardCombination> combinations)
	{
		int comboCount = combinations.Count;

		Debug.Log ("ATTACK Guards " + (DateTime.Now - turnStartTime).TotalMilliseconds + "  in miliseconds   combos" + combinations.Count);

		for (int i = 0; i < comboCount; i++)
		{
			if (DoesEnemyHaveGuard (combinations[i].dataContainer.enemyCards))
			{
				GetAttackers (availableToPlay, combinations[i], combinations[i].dataContainer.myCards);
				int count1 = availableToPlay.Count;
				for (int j = 0; j < count1; j++)
				{
					GenerateGuardAttackCombination (combinations[i], availableToPlay, combinations, j, combinations[i].dataContainer);
				}
			}
		}
	}

	private void GenerateCombination_Summons (List<Card> availableToPlay, List<CardCombination> combinations, CardCombination emptyCombination)
	{
		Debug.Log ("GenerateCombination_Summons");
		if (GetMyCreaturesOnBoard (dataContainer) < BOARD_LIMIT)
		{
			GenerateSummonCombination (emptyCombination, availableToPlay, 0, combinations, emptyCombination.dataContainer);
		}
	}

	private void GenerateCombination_Spells (List<Card> availableToPlay, List<CardCombination> combinations)
	{
		Debug.Log ("GenerateCombination_Spells");

		int comboCount = combinations.Count;
		for (int i = 0; i < comboCount; i++)
		{
			GetSpellsAvailableToUse (availableToPlay, combinations[i]);
			int count2 = availableToPlay.Count;
			for (int j = 0; j < count2; j++)
			{
				GenerateCastSpellCombination (combinations[i], availableToPlay, j, combinations, combinations[i].dataContainer);
			}
		}
	}

	private List<CardCombination> GenereateCombination_MyAttacks (List<Card> availableToPlay, List<CardCombination> combinations)
	{

		int currentComboCount = combinations.Count;
		Debug.Log ("ATTACK me " + (DateTime.Now - turnStartTime).TotalMilliseconds + "  in miliseconds   combos" + combinations.Count);
		for (int i = 0; i < currentComboCount; i++)
		{
			GetAttackers (availableToPlay, combinations[i], combinations[i].dataContainer.myCards);

			int count = availableToPlay.Count;
			for (int j = 0; j < count; j++)
			{
				GenerateAttackCombination (combinations[i], availableToPlay, combinations, j, combinations[i].dataContainer);
			}
		}

		return combinations;
	}

	private List<CardCombination> GenerateCombination_EnemyAttacks (List<Card> availableToPlay, List<CardCombination> combinations, List<CardCombination> enemyCombinations)
	{
		Debug.Log ("ATTACK enemy " + (DateTime.Now - turnStartTime).TotalMilliseconds + "  in miliseconds   combos" + combinations.Count);
		combinations = GetBestCombinations (combinations, Math.Max (1, combinations.Count));
		int currentComboCount = combinations.Count;
		List<CardCombination> bestEnemyCombo = new List<CardCombination> (1);

		for (int i = 0; i < currentComboCount; i++)
		{
			if ((DateTime.Now - turnStartTime).TotalMilliseconds > GetGeneratingTimeoutLimit (combinations))
			{
				if (i > Math.Max((float)combinations.Count / 10,20))
				{
					int trimStart = Math.Min (i + 1, combinations.Count - 1);
					combinations.RemoveRange (trimStart, combinations.Count - 1 - trimStart);
					Debug.Log ("Removed "+ (combinations.Count - 1 - trimStart)+" unchecked combos");
				}
				Debug.enabled = true;
				Debug.Log ("EMERGENCY BREAK GenerateCombination_EnemyAttacks " + (DateTime.Now - turnStartTime).TotalMilliseconds + "  totalCombos " + combinations.Count);

				throw new TimeOutException ();
			}

			enemyCombinations.Clear ();
			CardCombination enemyComboCopy = new CardCombination (combinations[i]);
			enemyComboCopy.dataContainer.ClearGameActions ();
			enemyCombinations.Add (enemyComboCopy);
			GetAttackers (availableToPlay, enemyComboCopy, enemyComboCopy.dataContainer.enemyCards);
			int count = availableToPlay.Count;
			for (int j = 0; j < count; j++)
			{
				GenerateAttackCombination (enemyComboCopy, availableToPlay, enemyCombinations, j, enemyComboCopy.dataContainer);
			}
			enemySimulations += enemyCombinations.Count;
			bestEnemyCombo = GetBestCombinations (enemyCombinations, enemyCombinations.Count, true);

			if (bestEnemyCombo.Count > 0 && bestEnemyCombo[0] != null)
			{
				GenerateScoreOffset (combinations[i]);

				//combinations[i].scoreOffset = ScoreCombination (bestEnemyCombo[0]);
				//combinations[i].scoreOffset += ScoreCombination (bestEnemyCombo[0]);
				combinations[i].dataContainer.myCards = bestEnemyCombo[0].dataContainer.myCards;
				combinations[i].dataContainer.enemyCards = bestEnemyCombo[0].dataContainer.enemyCards;
				combinations[i].dataContainer.me = bestEnemyCombo[0].dataContainer.me;
				combinations[i].dataContainer.enemy = bestEnemyCombo[0].dataContainer.enemy;
			}
		}

		return combinations;
	}

	private void GetAttackers (List<Card> availableToPlay, CardCombination combination, List<Card> cards)
	{
		availableToPlay.Clear ();
		for (int i = 0; i < cards.Count; i++)
		{
			if ((cards[i].location == (int)Card.CardLocation.Board || cards[i].location == (int)Card.CardLocation.Enemy) && !cards[i].tapped && cards[i].attack > 0)
			{
				availableToPlay.Add (cards[i]);
			}
		}
	}

	private void GetSpellsAvailableToUse (List<Card> availableToPlay, CardCombination cardCombination)
	{
		availableToPlay.Clear ();
		for (int i = 0; i < cardCombination.dataContainer.myCards.Count; i++)
		{
			if (cardCombination.dataContainer.myCards[i].cardType != (int)Card.CardType.Creature && cardCombination.dataContainer.myCards[i].cost <= cardCombination.dataContainer.me.mana)
			{
				availableToPlay.Add (cardCombination.dataContainer.myCards[i]);
			}
		}
	}

	private void GetCardsAvailableToSummon (List<Card> availableToPlay)
	{
		availableToPlay.Clear ();
		for (int i = 0; i < dataContainer.myCards.Count; i++)
		{
			if (dataContainer.myCards[i].cardType == (int)Card.CardType.Creature && dataContainer.myCards[i].location == (int)Card.CardLocation.Hand && dataContainer.myCards[i].cost <= dataContainer.me.startMana)
			{
				availableToPlay.Add (dataContainer.myCards[i]);
			}
		}
	}

	private void GenerateCastSpellCombination (CardCombination combination, List<Card> availableToPlay, int startIndex, List<CardCombination> combinations, DataContainer thisDataContainer)
	{
		if ((DateTime.Now - turnStartTime).TotalMilliseconds > GetGeneratingTimeoutLimit (combinations))
		{
			Debug.enabled = true;

			Debug.Log ("EMERGENCY BREAK GenerateCastSpellCombination " + (DateTime.Now - turnStartTime).TotalMilliseconds + "  totalCombos " + combinations.Count);
			throw new TimeOutException ();
		}
		if (startIndex >= availableToPlay.Count || availableToPlay[startIndex].cost > thisDataContainer.me.mana)
		{
			return;
		}
		const GameAction.ActionType action = GameAction.ActionType.USE;
		int count = availableToPlay.Count;
		if (availableToPlay[startIndex].cardType == (int)Card.CardType.BlueItem)
		{
			CardCombination copy = MakeNewCombination (combination, availableToPlay[startIndex], null, action);
			bool comboAdded = AddComboToList (combinations, copy);

			if (comboAdded)
			{
				copy.AddMana (availableToPlay[startIndex], action);
				ExecuteAction (copy.dataContainer.lastAction, copy.dataContainer);
				
						GenerateCastSpellCombination (copy, availableToPlay, startIndex + 1, combinations, copy.dataContainer);
			}
		}
		
		List<Card> targets = availableToPlay[startIndex].cardType == (int)Card.CardType.GreenItem ? thisDataContainer.myCards : thisDataContainer.enemyCards;
		int targetsCount = targets.Count;
		for (int j = 0; j < targetsCount; j++)
		{
			if (targets[j].location != (int)Card.CardLocation.Hand)
			{
				CardCombination copy = MakeNewCombination (combination, availableToPlay[startIndex], targets[j], action);
				bool comboAdded = AddComboToList (combinations, copy);

				if (comboAdded)
				{
					copy.AddMana (availableToPlay[startIndex], action);
					ExecuteAction (copy.dataContainer.lastAction, copy.dataContainer);
					
					GenerateCastSpellCombination (copy, availableToPlay, startIndex + 1, combinations, copy.dataContainer);
				}
			}
		}
	}

	private static bool AddComboToList (List<CardCombination> combinations, CardCombination copy)
	{
			combinations.Add (copy);
		return true;
	}

	private void GenerateAttackCombination (CardCombination combination, List<Card> availableToPlay, List<CardCombination> combinations, int startIndex, DataContainer thisDataContainer)
	{
		if ((DateTime.Now - turnStartTime).TotalMilliseconds > GetGeneratingTimeoutLimit (combinations))
		{
			Debug.enabled = true;

			Debug.Log ("EMERGENCY BREAK GenerateAttackCombination " + (DateTime.Now - turnStartTime).TotalMilliseconds + "  totalCombos " + combinations.Count);

			throw new TimeOutException ();
		}
		int count = availableToPlay.Count;
		if (startIndex >= count || availableToPlay[startIndex] == null)
		{
			return;
		}

		Card thisCard = GetCard (availableToPlay[startIndex].instanceId, thisDataContainer); 
		if (thisCard == null || thisCard.tapped)
		{
			return;
		}
		List<Card> enemyCards = thisCard.player == thisDataContainer.me ? thisDataContainer.enemyCards : thisDataContainer.myCards;
		const GameAction.ActionType action = GameAction.ActionType.ATTACK;
		CardCombination copy = null;
		bool comboAdded = false;
		bool enemyHasGuards = DoesEnemyHaveGuard (enemyCards);

		
		List<Card> targets = availableToPlay[startIndex].location == (int)Card.CardLocation.Enemy ? thisDataContainer.myCards : thisDataContainer.enemyCards;

		int targetsCount = targets.Count;

		for (int j = 0; j < targetsCount; j++)
		{
			if (targets[j] != null && (targets[j].IsGuard () || !enemyHasGuards) && (targets[j].location == (int)Card.CardLocation.Board || targets[j].location == (int)Card.CardLocation.Enemy))
			{
				copy = MakeNewCombination (combination, availableToPlay[startIndex], targets[j], action);
				//Card newAvailable2 = GetCard (availableToPlay[startIndex].instanceId, copy.dataContainer);
				//if (newAvailable2 != null)
				//{
				comboAdded = AddComboToList (combinations, copy);
				if (comboAdded)
				{
					copy.AddMana (availableToPlay[startIndex], action);
					ExecuteAction (copy.dataContainer.lastAction, copy.dataContainer);
					for (int i = 0; i < count; i++)
					{
						Card newAvailable = GetCard (availableToPlay[i].instanceId, copy.dataContainer);
						if (newAvailable != null && !newAvailable.tapped && newAvailable.attack > 0)
						{
							GenerateAttackCombination (copy, availableToPlay, combinations, i, copy.dataContainer);
						}
					}
				}
				//}
			}
		}

		if (!enemyHasGuards)
		{
			copy = MakeNewCombination (combination, availableToPlay[startIndex], null, action);
			//Card tmpC = GetCard (availableToPlay[startIndex].instanceId, copy.dataContainer);
			//if (tmpC != null)
			//{
			comboAdded = AddComboToList (combinations, copy);
			if (comboAdded)
			{

				copy.AddMana (availableToPlay[startIndex], action);
				ExecuteAction (copy.dataContainer.lastAction, copy.dataContainer);

				for (int i = 0; i < count; i++)
				{
					Card newAvailable = GetCard (availableToPlay[i].instanceId, copy.dataContainer);
					if (newAvailable != null && !newAvailable.tapped && newAvailable.attack > 0)
					{
						GenerateAttackCombination (copy, availableToPlay, combinations, i, copy.dataContainer);
					}
				}

			}
		}
	}

	private void GenerateGuardAttackCombination (CardCombination combination, List<Card> availableToPlay, List<CardCombination> combinations, int startIndex, DataContainer thisDataContainer)
	{
		if ((DateTime.Now - turnStartTime).TotalMilliseconds > GetGeneratingTimeoutLimit (combinations))
		{
			Debug.enabled = true;

			Debug.Log ("EMERGENCY BREAK GenerateGuardAttackCombination " + (DateTime.Now - turnStartTime).TotalMilliseconds + "  totalCombos " + combinations.Count);

			throw new TimeOutException ();
		}
		int count = availableToPlay.Count;

		if (startIndex >= count)
		{
			return;
		}
		Card thisCard = GetCard (availableToPlay[startIndex].instanceId, thisDataContainer);
		if (thisCard == null || thisCard.tapped)
		{
			return;

		}
		const GameAction.ActionType action = GameAction.ActionType.ATTACK;
		CardCombination copy = null;
		bool comboAdded = false;
		bool enemyHasGuards = DoesEnemyHaveGuard (thisDataContainer.enemyCards);

		if (!enemyHasGuards)
		{
			return;
		}

		List<Card> targets = availableToPlay[startIndex].location == (int)Card.CardLocation.Enemy ? thisDataContainer.myCards : thisDataContainer.enemyCards;
		int targetsCount = targets.Count;
		for (int j = 0; j < targetsCount; j++)
		{
			if (targets[j] != null && (targets[j].IsGuard ()))
			{
				copy = MakeNewCombination (combination, availableToPlay[startIndex], targets[j], action);
				comboAdded = AddComboToList (combinations, copy);

				if (comboAdded)
				{
					copy.AddMana (availableToPlay[startIndex], action);
					ExecuteAction (copy.dataContainer.lastAction, copy.dataContainer);
					for (int i = 0; i < count; i++)
					{
						Card newAvailable = GetCard (availableToPlay[i].instanceId, copy.dataContainer);
						if (newAvailable != null && !newAvailable.tapped && newAvailable.attack > 0)
						{
							GenerateGuardAttackCombination (copy, availableToPlay, combinations, i, copy.dataContainer);
						}
					}
				}
			}
		}
	}

	bool DoesEnemyHaveGuard (List<Card> enemyCards)
	{
		for (int i = 0; i < enemyCards.Count; i++)
		{
			int loc = enemyCards[i].location;
			if (loc != (int)Card.CardLocation.Hand && enemyCards[i].IsGuard ())
			{
				return true;
			}
		}
		return false;
	}

	Card GetBestCard (List<Card> availableCards)
	{
		Card best = null;
		float bestPick = float.MinValue;
		for (int i = 0; i < availableCards.Count; i++)
		{
			float currPickVal = ScorePick (availableCards[i]);
			Debug.Log (availableCards[i].ToString () + " " + currPickVal);
			if (bestPick < currPickVal)
			{
				best = availableCards[i];
				bestPick = currPickVal;
			}
		}
		return best;
	}

	public void GenerateScoreOffset (CardCombination combo)
	{
		float score = 0;
		for (int i = 0; i < combo.dataContainer.enemyCards.Count; i++)
		{
			score += combo.dataContainer.enemyCards[i].attack * 4;
			score += combo.dataContainer.enemyCards[i].defense;
			score += ScoreCreatureOnBoardComboStyle (combo.dataContainer.enemyCards[i]) * 3;
		}
		combo.scoreOffset = -score / 15; //IS: chce delikatnie zoffsetowac zeby
	}

	private float ScorePick (Card card)
	{
		float score = card.GetValue ();
		//Debug.Log ("dataContainer.pickedCards.Count " + pickedCards.Count + " " + PICK_CURVE_STABILIZATION_THRESHOLD);
		//if (pickedCards.Count > PICK_CURVE_STABILIZATION_THRESHOLD)
		//{
		//	int cost = 0;
		//	int[] costsCount = new int[8];
		//	for (int i = 0; i < pickedCards.Count; i++)
		//	{
		//		cost = Math.Min (7, pickedCards[i].cost);
		//		costsCount[cost]++;
		//	}
		//	cost = Math.Min (7, card.cost);
		//	float avgVal = ((float)CARDS_IN_DECK / costsCount.Length) / CARDS_IN_DECK;
		//	float multi = (float)Math.Max (costsCount[cost], 1) / (CARDS_IN_DECK);
		//	multi = avgVal / multi;
		//	multi -= Math.Sign (1 - multi) * Math.Min (0.33f, Math.Abs (1 - multi));
		//	if (cost <= 1)
		//	{
		//		multi = Math.Max (-1.5f, Math.Min (1.5f, multi));
		//	}
		//	Debug.Log ("multi " + multi + "  score  " + score + "    score  * multi " + (score * multi));
		//	score *= multi;
		//	score += cost / 10;
		//}
		return score;
	}

	public static Card GetCard (int id, DataContainer combinationContainer)
	{
		if (combinationContainer.cardDictionary.ContainsKey (id))
		{
			return combinationContainer.cardDictionary[id];
		}
		//for (int i = 0; i < combinationContainer.myCards.Count; i++)
		//{
		//	if (combinationContainer.myCards[i].instanceId == id)
		//	{
		//		return combinationContainer.myCards[i];
		//	}
		//}

		//for (int i = 0; i < combinationContainer.enemyCards.Count; i++)
		//{
		//	if (combinationContainer.enemyCards[i].instanceId == id)
		//	{
		//		return combinationContainer.enemyCards[i];
		//	}
		//}
		return null;
	}

	void ExecuteAction (GameAction action, DataContainer combinationContainer)
	{
		Card executioner = null;
		Card target = null;

		switch (action.actionType)
		{
			case GameAction.ActionType.SUMMON:
				executioner = GetCard (action.actonExecuter, combinationContainer);
				executioner.location = (int)Card.CardLocation.Board;
				executioner.player.mana -= executioner.cost;
				if (!executioner.IsCharge ())
				{
					executioner.tapped = true;
				}
				OnCardEnter (executioner, combinationContainer);
				break;
			case GameAction.ActionType.ATTACK:

				executioner = GetCard (action.actonExecuter, combinationContainer);
				target = GetCard (action.actonTarget, combinationContainer);
				OnAttack (executioner, target, combinationContainer);


				break;
			case GameAction.ActionType.PASS:
				break;
			case GameAction.ActionType.PICK:

				break;
			case GameAction.ActionType.USE:
				executioner = GetCard (action.actonExecuter, combinationContainer);
				executioner.player.mana -= executioner.cost;
				target = GetCard (action.actonTarget, combinationContainer);
				CastSpell (executioner, target, combinationContainer);
				break;
			default:
				break;
		}

		bool removed = false;
		List<Card> executionerCards = combinationContainer.enemyCards.Contains (executioner) ? combinationContainer.enemyCards : combinationContainer.myCards;
		List<Card> targetCards = null;
		if (target != null)
		{

			if (executionerCards == combinationContainer.enemyCards)
			{
				//Debug.Log ("target my cards");
				targetCards = combinationContainer.myCards;
			} else
			{
				//Debug.Log ("target enemy cards");
				targetCards = combinationContainer.enemyCards;
			}

			if (target.defense <= 0)
			{
				removed = targetCards.Remove (target);
				if (!removed)
				{
					removed = targetCards.Remove (GetCard (target.instanceId, combinationContainer));
				}
			}
		}
		if (executioner.defense <= 0 || executioner.cardType != (int)Card.CardType.Creature)
		{
			removed = executionerCards.Remove (executioner);
			if (!removed)
			{
				removed = executionerCards.Remove (GetCard (executioner.instanceId, combinationContainer));
			}

		}
	}

	private void CastSpell (Card executioner, Card target, DataContainer tmpContainer)
	{
		Player caster = executioner.player;
		Player castersEnemy = caster == tmpContainer.me ? tmpContainer.enemy : tmpContainer.me;

		caster.health += executioner.myHealthChange;
		castersEnemy.health += executioner.opponentHealthChange;
		if (caster == tmpContainer.me)
		{
			tmpContainer.drawCount += executioner.cardDraw;
		}

		if (target != null)
		{
			if (executioner.cardType == (int)Card.CardType.RedItem || executioner.cardType == (int)Card.CardType.BlueItem)
			{
				
				target.bAbilities = (byte)(target.bAbilities & ~executioner.bAbilities);
			} else
			{
				
				target.bAbilities = (byte)(target.bAbilities | executioner.bAbilities);
			}

			if (executioner.cardType == (int)Card.CardType.RedItem && (executioner.attack < 0 || executioner.defense < 0) && target.IsWard ())
			{
				target.UseWard ();
			} else
			{
				target.attack += executioner.attack;
				target.defense += executioner.defense;
			}
		}
	}

	private void OnAttack (Card executioner, Card target, DataContainer combinationContainer)
	{
		Player executionerPlayer = executioner.player;
		Player targetPlayer = executioner.player == combinationContainer.me ? combinationContainer.enemy : combinationContainer.me;

		if (target != null)
		{
			if (executioner.attack > 0)
			{
				if (target.IsWard ())
				{
					target.UseWard ();
				} else
				{
					if (executioner.IsLethal ())
					{
						target.defense = 0;
					} else
					{
						target.defense -= executioner.attack;
					}
					if (target.defense <= 0)
					{
						if (executioner.IsBreakthrough ())
						{
							targetPlayer.health -= Math.Abs (target.defense);
						}
					}
				}
			}
			if (target.attack > 0)
			{
				if (executioner.IsWard ())
				{
					executioner.UseWard ();
				} else
				{
					if (target.IsLethal ())
					{
						executioner.defense = 0;
					} else
					{
						executioner.defense -= target.attack;
					}

					if (executioner.IsDrain ())
					{
						executionerPlayer.health += executioner.attack;
					}

				}
			}

		} else
		{
			targetPlayer.health -= executioner.attack;
		}

		executioner.tapped = true;
	}

	private void OnCardEnter (Card toSummon, DataContainer combinationContainer)
	{
		combinationContainer.me.health += toSummon.myHealthChange;
		combinationContainer.enemy.health += toSummon.opponentHealthChange;
		combinationContainer.drawCount += toSummon.cardDraw;
	}
}

class CardCombination
{
	public DataContainer dataContainer;
	public int manaCost = 0;
	public float scoreOffset = 0;

	public CardCombination (CardCombination otherCombination)
	{
		dataContainer = new DataContainer (otherCombination.dataContainer);
	}

	public CardCombination ()
	{
	}

	public float GetManaCost ()
	{
		return manaCost;
	}



	public bool AreDifferent (CardCombination combination)
	{

		bool v = dataContainer.IsDifferent (combination.dataContainer);
		return v;
	}

	internal void AddMana (Card card, GameAction.ActionType actionType)
	{
		if (actionType == GameAction.ActionType.SUMMON || actionType == GameAction.ActionType.USE)
		{
			if (card != null)
			{
				manaCost += card.cost;
			} else
			{
				Debug.Log ("card is null");
			}
		}
	}

	internal void PrintActions ()
	{
		Debug.Log ("printing actions");
		for (int i = 0; i < dataContainer.gameActions.Count; i++)
		{
			Debug.Log ("== " + dataContainer.gameActions[i].ToString ());
		}
	}

	internal string GetActionsOneLine ()
	{
		string act = "actions: ";
		for (int i = 0; i < dataContainer.gameActions.Count; i++)
		{
			act += dataContainer.gameActions[i].ToString ()+"; ";
		}
		return act;
	}
}

class GameAction
{
	public enum ActionType { SUMMON, ATTACK, PICK, USE, PASS }
	public ActionType actionType;
	public int actonExecuter;
	public int actonTarget;
	public ushort actionHash;

	public GameAction (ActionType actionType, int actonExecuter = -2, int actonTarget = -2)
	{
		this.actionType = actionType;
		this.actonExecuter = actonExecuter;
		this.actonTarget = actonTarget;

		GenerateHash ();
	}

	private void GenerateHash ()
	{
		ushort hashedExecutioners = (ushort)((actonExecuter < 0 ? 63 : actonExecuter) << 11);
		ushort hashedTarget = (ushort)((actonTarget < 0 ? 63 : actonTarget) << 6);

		actionHash = (ushort)(hashedExecutioners | hashedTarget | (int)actionType);
	}

	public override string ToString ()
	{
		string toReturn = actionType.ToString ();

		if (actonExecuter != -2)
		{
			toReturn += " " + actonExecuter;
			if (actonTarget != -2)
			{
				toReturn += " " + actonTarget;
			}
		}
		return toReturn;//actionType.ToString() + " " + (actonExecuter != -2 ? actonExecuter.ToString() : "") + " " + (actonTarget != -2 ? actonTarget.ToString() : "");
	}
}
class DataContainer
{
	static int staticId = 0;
	public int id;
	public Player me;
	public Player enemy;

	public List<Card> myCards = new List<Card> ();
	public List<Card> enemyCards = new List<Card> ();
	public List<GameAction> gameActions = new List<GameAction> ();
	public Dictionary<int, Card> cardDictionary = new Dictionary<int, Card> ();

	public int drawCount = 0;
	public GameAction lastAction;


	internal void Clear ()
	{
		myCards.Clear ();
		enemyCards.Clear ();
		ClearGameActions ();
		staticId = 0;
	}

	public void ClearGameActions ()
	{
		gameActions.Clear ();
		gameActionsHashes.Clear ();
	}

	public GameAction AddAction (GameAction.ActionType type, Card executer = null, Card target = null)
	{
		return AddAction (type, executer != null ? executer.instanceId : -2, target != null ? target.instanceId : type == GameAction.ActionType.ATTACK || type == GameAction.ActionType.USE ? -1 : -2);
	}
	public GameAction AddAction (GameAction.ActionType type, int executer = -2, int target = -2)
	{
		GameAction newAction = new GameAction (type, executer, target);
		gameActions.Add (newAction);
		lastAction = newAction;

		gameActionsHashes.Add (newAction.actionHash);
		return newAction;
	}


	public DataContainer (DataContainer template) : this ()
	{
		cardDictionary = new Dictionary<int, Card> (template.enemyCards.Count + template.myCards.Count);
		me = new Player (template.me);
		enemy = new Player (template.enemy);

		enemyCards = new List<Card> (template.enemyCards.Count);
		myCards = new List<Card> (template.myCards.Count);

		CopyCards (template.enemyCards, enemyCards);
		CopyCards (template.myCards, myCards);

		for (int i = 0; i < template.gameActions.Count; i++)
		{
			AddAction (template.gameActions[i].actionType, template.gameActions[i].actonExecuter, template.gameActions[i].actonTarget);
		}
		drawCount = template.drawCount;
	}

	public DataContainer ()
	{
		id = staticId++;
	}

	private void CopyCards (List<Card> cards, List<Card> targetList)
	{
		for (int i = 0; i < cards.Count; i++)
		{
			if (cards[i] != null)
			{
				Card cardCopy = new Card (cards[i]);
				cardCopy.player = cardCopy.location == (int)Card.CardLocation.Enemy ? enemy : me;

				targetList.Add (cardCopy);
				cardDictionary.Add (cardCopy.instanceId, cardCopy);
			} else
			{
				Debug.Log ("null when copying cards");
			}
		}
	}
	HashSet<ushort> gameActionsHashes = new HashSet<ushort> ();
	//List<ushort> gameActionsHashes = new List<ushort> ();
	internal bool IsDifferent (DataContainer dataContainer)
	{
		if (gameActions.Count == dataContainer.gameActions.Count)
		{

			bool v = !gameActionsHashes.SetEquals (dataContainer.gameActionsHashes);
			//bool v = gameActionsHashes.Except (dataContainer.gameActionsHashes).Any();
			return v;
			//{
			//	if (gameActions[i].actionType != dataContainer.gameActions[i].actionType || gameActions[i].actonExecuter != dataContainer.gameActions[i].actonExecuter || gameActions[i].actonTarget != dataContainer.gameActions[i].actonTarget)
			//	{
			//		return true;
			//	}
			//}
		} else
		{
			return true;
		}


		return false;
	}
}

class Player
{
	public Player (string input)
	{
		string[] inputs;
		inputs = input.Split (' ');
		health = int.Parse (inputs[0]);
		mana = int.Parse (inputs[1]);
		deck = int.Parse (inputs[2]);
		rune = int.Parse (inputs[3]);
		startMana = mana;
		initialInput = input;
	}

	public Player (Player template)
	{
		deck = template.deck;
		rune = template.rune;
		startMana = template.startMana;
		health = template.health;
		mana = template.mana;
		initialInput = template.initialInput;

	}

	public Player ()
	{

	}

	string initialInput;
	public int health;
	public int mana;
	public int deck;
	public int rune;
	public int startMana;
}

class Card
{
	public Card (string input)
	{
		string[] inputs = input.Split (' ');

		cardNumber = int.Parse (inputs[0]);
		instanceId = int.Parse (inputs[1]);
		location = int.Parse (inputs[2]);
		cardType = int.Parse (inputs[3]);
		cost = int.Parse (inputs[4]);
		attack = int.Parse (inputs[5]);
		defense = int.Parse (inputs[6]);
		abilities = inputs[7];
		myHealthChange = int.Parse (inputs[8]);
		opponentHealthChange = int.Parse (inputs[9]);
		cardDraw = int.Parse (inputs[10]);
		ConvertAbilities ();

		value = SummonValue ();

		initialInput = input;
	}

	private void ConvertAbilities ()
	{
		bAbilities = 0;

		if (abilities.Contains ("G"))
		{
			bAbilities += GUARD_PATTERN;
		}
		if (abilities.Contains ("W"))
		{
			bAbilities += WARD_PATTERN;
		}
		if (abilities.Contains ("B"))
		{
			bAbilities += BREAKTHROUGH_PATTERN;
		}
		if (abilities.Contains ("C"))
		{
			bAbilities += CHARGE_PATTERN;
		}
		if (abilities.Contains ("D"))
		{
			bAbilities += DRAIN_PATTERN;
		}
		if (abilities.Contains ("L"))
		{
			bAbilities += LETHAL_PATTERN;
		}
	}

	public Card (Card template)
	{
		cardNumber = template.cardNumber;
		instanceId = template.instanceId;
		cardType = template.cardType;
		cost = template.cost;
		myHealthChange = template.myHealthChange;
		opponentHealthChange = template.opponentHealthChange;
		cardDraw = template.cardDraw;

		defense = template.defense;
		attack = template.attack;
		tapped = template.tapped;
		location = template.location;
		abilities = template.abilities;
		bAbilities = template.bAbilities;
		value = SummonValue ();
	}

	public Card ()
	{

	}
	public enum CardLocation { Hand = 0, Board = 1, Enemy = -1 }
	public enum CardType { Creature = 0, GreenItem = 1, RedItem = 2, BlueItem = 3 }

	string initialInput;

	public Player player;
	public int cardNumber;
	public int instanceId;
	public int location;
	public int cardType;
	public int cost;
	public int attack;
	public int defense;
	public string abilities;
	public int myHealthChange;
	public int opponentHealthChange;
	public int cardDraw;
	float value = -1;
	internal bool tapped = false;
	
	public float PickValue ()
	{
		int val = 0;
		switch (cardType)
		{
			case 0:
				val = ((Math.Abs (attack) - (cost * 2) + (myHealthChange / 2) + (-opponentHealthChange) + (cardDraw / 2) + (IsCharge () ? attack : 0)) * (IsDrain () ? 2 : 1));
				break;
			case 1:
				val = (Math.Abs ((attack) + Math.Abs (defense)) / cost) + myHealthChange + (-opponentHealthChange) + cardDraw;
				break;
			case 2:
				break;
			case 3:
				break;
			default:
				break;

		}
		return val;
	}


	public float SummonValue ()
	{

		//float baseCost = (float)(attack + defense) / cost;
		//return baseCost  * ((myHealthChange / 2) + opponentHealthChange + ((float)cardDraw / 2) + (IsCharge() ? attack : 0)) * (IsDrain() ? 2 : 1);
		float val = (float)(Math.Abs (attack * 2) + Math.Abs (defense * (IsCreature () ? 1 : (cardType == (int)CardType.RedItem || cardType == (int)CardType.BlueItem) ? 3 : 0.5f))) + (-opponentHealthChange) + (cardDraw * 3) + Game.ScoreCreatureOnBoard (this, true);
		val +=  (IsGuard () ? defense : 0);

		if (cardType != (int)CardType.RedItem && cardType != (int)CardType.BlueItem)
		{
			if (IsCreature ())
			{
				val += ((IsWard () ? 30 : 0) + (IsCharge () ? attack * 3 : 0) + (IsLethal () ? 50 : 0));
			} else
			{
				val += ((IsWard () ? attack  * 2: 0) + (IsLethal () ? 30 : 0));
			}
		} else {
			val += Math.Abs (defense) * 3;
			val += (IsWard () ? Math.Abs (defense) : (IsLethal () ? 5 : (IsGuard () ? 5 : 0)));
		}

		val /= Math.Max (cost, 1.5f);
		if (IsCreature() && attack <= 0)
		{
			val /= 3;
		}

		//Debug.Log ("id "+instanceId+" "+ (Math.Abs (attack) + Math.Abs (defense * (IsCreature () ? 1 : 0.5f)))+" "+((myHealthChange > 0 ? 1 : 0) + (-opponentHealthChange) + cardDraw)+" "+ (Game.ScoreCreatureOnBoard (this, true) + (IsCharge () ? attack : 0) + (IsLethal () ? attack : 0)));
		return val;
	}

	public float GetValue ()
	{
		return value;
	}

	const byte GUARD_PATTERN = 1;
	const byte BREAKTHROUGH_PATTERN = 2;
	const byte LETHAL_PATTERN = 4;
	const byte WARD_PATTERN = 8;
	const byte CHARGE_PATTERN = 16;
	const byte DRAIN_PATTERN = 32;
	public byte bAbilities = 0;

	internal bool IsGuard ()
	{
		return ContainsAbility (GUARD_PATTERN);
	}

	internal bool IsCharge ()
	{
		return ContainsAbility (CHARGE_PATTERN);
	}

	internal bool IsDrain ()
	{
		return ContainsAbility (DRAIN_PATTERN);
	}

	internal bool IsWard ()
	{
		return ContainsAbility (WARD_PATTERN);
	}
	internal bool IsLethal ()
	{
		return ContainsAbility (LETHAL_PATTERN);
	}
	internal bool IsBreakthrough ()
	{
		return ContainsAbility (BREAKTHROUGH_PATTERN);
	}

	private bool ContainsAbility (byte ability)
	{
		bool v = (bAbilities & ability) == ability;
		return v;
	}

	internal bool IsCreature ()
	{
		return cardType == 0;
	}

	internal void UseWard ()
	{
		bAbilities = (byte)(bAbilities & ~WARD_PATTERN);
	}


	public override string ToString ()
	{
		string toRet = "";
		toRet += instanceId + " a:" + attack + " d:" + defense + " v:" + value + " eC" + opponentHealthChange+" "+(IsLethal()?"L":"") + " " + (IsCharge () ? "C" : "") + " " + (IsBreakthrough () ? "B" : "") + " " + (IsDrain () ? "D" : "") + " " + (IsWard () ? "W" : "") + " " + (IsGuard () ? "G" : "");
		return toRet;
	}
}
class Debug
{
	public static void Log (object log)
	{

#if PROFILE
		if (enabled)
		{
			Console.Error.WriteLine (log);

		}
#endif
	}

	static Dictionary<string, float> profiler = new Dictionary<string, float> ();
	static Dictionary<string, int> profilerCount = new Dictionary<string, int> ();
	static Stack<KeyValuePair<string, DateTime>> profilerStart = new Stack<KeyValuePair<string, DateTime>> ();

	public static bool enabled { get; internal set; }

	public static void ProfilerStart (string sampleName)
	{
#if PROFILE
		profilerStart.Push (new KeyValuePair<string, DateTime> (sampleName, DateTime.Now));
#endif
	}

	public static void ProfilerEnd ()
	{
#if PROFILE
		if (profilerStart.Count == 0)
		{
			return;
		}
		KeyValuePair<string, DateTime> fifo = profilerStart.Pop ();
		float totalMilliseconds = (float)(DateTime.Now - fifo.Value).TotalMilliseconds;

		if (profiler.ContainsKey (fifo.Key))
		{
			profiler[fifo.Key] += totalMilliseconds;
			profilerCount[fifo.Key]++;
		} else
		{
			profiler.Add (fifo.Key, totalMilliseconds);
			profilerCount.Add (fifo.Key, 1);
		}
#endif
	}

	public static void PrintProfiler ()
	{
#if PROFILE
		Log ("===Profiler===");
		foreach (var item in profiler.Keys)
		{
			Log (item + " " + profiler[item] + " ms  -  " + profilerCount[item] + " times");
		}
#endif
	}

	public static void Clear ()
	{
		ClearProfiler ();
	}

	public static void ClearProfiler ()
	{
		profiler.Clear ();
		profilerStart.Clear ();
		profilerCount.Clear ();
	}
}
class TimeOutException : Exception
{

}