/*Yanbo(Amber) Shao*/

/*Game Name: Poker Duel */

/*
* Introduction
*
* A two-player poker-based duel game similar to UNO. Players take turns playing cards. If they play cards of a suit that does not meet the requirements, they will lose life points. If the life points return to 0, the game fails.
*
* - Create and manage a deck of 54 cards (52 standard cards + 2 jokers)
* - Support dual modes: player vs player / player vs computer
* - Implement a special card effect system based on suit and points
* - Turn-based game process management system
* - Dynamic life value management system
*/

/*
* Code has variables:
* "deck" – Main deck instance
* "p1Hand" – Player 1's hand cards
* "p2Hand" – Player 2/Computer's hand cards
* "p1Life" – Player 1's HP (initial 15)
* "p2Life" – Player 2's HP (initial 15)
* "currentSuit" – Currently enforced follow-suit
* "isPlayer1Turn" – Turn control flag
* "vsComputer" –  Game mode flag (true=PvE)
* "blockDraw" – Draw blocking state flag
* "restrictNumbers" – Number card restriction flag
*/

/*
* Special card mechanism
* Ace of Hearts: When the Ace of Hearts is played, the opponent's life value is reduced by 2 points.
* Ace of Diamonds: When the Ace of Diamonds is played, the target player cannot draw any cards in the next round.
* Ace of Spades: When the Ace of Spades is played, the opponent can only play low-value cards in this round.
* Ace of Clubs: When the Ace of Clubs is played, the player's life value increases by 2 points.
* JOKER: Can replace any card of any suit
*/

/*
* Win Conditions
* (One of them can be met)
* 1. If one player loses all his health points during the game, the other player wins;
* 2. If both players have health points at the end of the game, the one with higher health points wins;
*/

using System;
using System.Collections.Generic;
using System.Linq;

namespace PokerDuel
{
    /*
     * Poker Suits Enumeration
     * 
     * Contains standard poker suits and special joker cards:
     * - Heart
     * - Spades
     * - Diamonds
     * - Clubs
     * - Joker
     */
    public enum Suit { Hearts, Spades, Diamonds, Clubs, Joker }

    /*
     * Card Class
     * 
     * Core Properties:
     * - Suit
     * - Rank）
     * - Value
     * - Joker
     */
    public class Card
    {
        public Suit Suit { get; }     
        public string Rank { get; }   
        public int Value { get; }     
        public bool IsJoker { get; }  //Joker flag

        /* Regular Card Constructor
         * Parameters:
         * - Suit (cannot be Joker)
         * - rank: A,2-10,J,Q,K
         */
        public Card(Suit suit, string rank)
        {
            Suit = suit;
            Rank = rank;
            IsJoker = false;

            //Value conversion logic
            Value = rank switch
            {
                "A" => 1,           //Ace as 1 point
                "J" or "Q" or "K" => 10,  //Face cards as 10 points
                _ => int.Parse(rank)      //Numeric cards direct conversion
            };
        }

        /* Creates special joker card*/
        public Card()
        {
            Suit = Suit.Joker;
            Rank = "JOKER";      // Special rank designation
            Value = 0;           // No comparative value
            IsJoker = true;      // Set joker flag
        }

        /* String Representation Method*/
        public override string ToString()
        {
            return IsJoker ? "JOKER" : $"{GetSuitChar()}{Rank}";
        }

        /* Suit Character Converter
         * Converts enum to single-character representation:
         * - Hearts → H 
         * - Spades → S 
         * - Diamonds → D 
         * - Clubs → C 
         */
        private char GetSuitChar()
        {
            return Suit switch
            {
                Suit.Hearts => 'H',
                Suit.Spades => 'S',
                Suit.Diamonds => 'D',
                Suit.Clubs => 'C',
                _ => ' '  // No character for Joker
            };
        }
    }

    /*
     * Deck Manager Class
     * - Auto-generates complete deck
     * - Shuffling mechanism
     * - Card drawing management
     */
    public class Deck
    {
        private List<Card> cards = new List<Card>();  // Card storage list
        private Random random = new Random();         // Random number generator

        /* Generates 52 standard + 2 jokers*/
        public Deck()
        {
            // Generate four standard suits
            foreach (Suit suit in Enum.GetValues(typeof(Suit)))
            {
                if (suit == Suit.Joker) continue;
                // 13 cards per suit (A,2-10,J,Q,K)
                var ranks = new[] { "A", "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K" };
                foreach (var rank in ranks)
                {
                    cards.Add(new Card(suit, rank));
                }
            }

            //Add two jokers
            cards.Add(new Card());
            cards.Add(new Card());
            Shuffle();  // Initial shuffle
        }

        /* Shuffle Method*/
        public void Shuffle()
        {
            for (int i = 0; i < cards.Count; i++)
            {
                int swapIndex = random.Next(cards.Count);
                (cards[i], cards[swapIndex]) = (cards[swapIndex], cards[i]);
            }
        }

        /* Draw Method
         * Returns top card from deck
         * Removes card from deck
         */
        public Card Draw() => cards.Count > 0 ? RemoveCard(0) : null;

        private Card RemoveCard(int index)
        {
            var card = cards[index];
            cards.RemoveAt(index);
            return card;
        }

        // Remaining Cards Property
        public int Count => cards.Count;
    }

    /*
     * Main Game Class
     * Contains game state and core logic:
     * - Player hand management
     * - HP system
     * - Game flow control
     */
    class Program
    {
        // Game components
        static Deck deck = new Deck();              // Main deck
        static List<Card> p1Hand = new List<Card>();  // Player 1's hand
        static List<Card> p2Hand = new List<Card>();  // Player 2's hand
        static int p1Life = 15, p2Life = 15;          // Player HP (initial 15)

        // Game state variables
        static Suit currentSuit;       // Currently required suit
        static bool isPlayer1Turn;      // Turn indicator
        static bool vsComputer;         // Game mode flag
        static bool blockDraw;         // Draw blocking flag
        static bool restrictNumbers;    // Number card restriction flag

        static void Main()
        {
            ShowTitle();   // Display title and mode selection
            InitGame();    // Game initialization
            GameLoop();    // Enter main loop
        }

        /* Display Title Screen */
        static void ShowTitle()
        {
            Console.WriteLine("======== POKER DUEL ========");
            Console.WriteLine("1. Player vs Player");
            Console.WriteLine("2. Player vs Computer");
            Console.WriteLine("============================");
            vsComputer = GetChoice(1, 2) == 2;  // Get mode selection
        }

        //Game Initialization
        static void InitGame()
        {
            // Initial deal (4 cards each)
            for (int i = 0; i < 4; i++)
            {
                p1Hand.Add(deck.Draw());
                p2Hand.Add(deck.Draw());
            }

            Console.WriteLine("\n=== INITIAL CARD SELECT ===");
            var (p1Card, p2Card) = SelectInitialCards();
            isPlayer1Turn = CompareInitialCards(p1Card, p2Card);
            
            // Remove used initial cards
            p1Hand.Remove(p1Card);
            p2Hand.Remove(p2Card);
        }

        /* Initial Card Selection Process */
        static (Card, Card) SelectInitialCards()
        {
            // Player 1 selection
            Console.WriteLine("Player 1's hand:");
            DisplayHand(p1Hand);
            int p1Choice = GetChoice(1, p1Hand.Count) - 1;

            // Player 2/Computer selection
            int p2Choice = vsComputer ? new Random().Next(p2Hand.Count) : GetPlayer2Choice();

            return (p1Hand[p1Choice], p2Hand[p2Choice]);
        }

        /* Get Player 2 Choice */
        static int GetPlayer2Choice()
        {
            Console.WriteLine("\nPlayer 2's hand:");
            DisplayHand(p2Hand);
            return GetChoice(1, p2Hand.Count) - 1;
        }

        /* Initial Card Comparison Logic
         * Return:
         * - true: Player 1 goes first
         * - false: Player 2/Computer goes first
         */
        static bool CompareInitialCards(Card p1, Card p2)
        {
            Console.WriteLine($"\nPlayer 1's card: {p1}");
            Console.WriteLine($"Player 2's card: {p2}");

            // Value comparison
            int result = p1.Value.CompareTo(p2.Value);
            if (result == 0)
            {
                // Suit priority: Spades > Hearts > Diamonds > Clubs
                var suitOrder = new[] { Suit.Spades, Suit.Hearts, Suit.Diamonds, Suit.Clubs };
                result = Array.IndexOf(suitOrder, p1.Suit).CompareTo(Array.IndexOf(suitOrder, p2.Suit));
            }

            bool p1First = result >= 0;
            Console.WriteLine(p1First ? "\nPlayer 1 goes first!" : "\nPlayer 2 goes first!");
            return p1First;
        }

        /* Main Game Loop */
        static void GameLoop()
        {
            while (!GameOver())
            {
                Console.Clear();
                DrawPhase();     
                ShowStatus();    
                if (isPlayer1Turn) PlayerTurn(p1Hand, ref p1Life, ref p2Life);
                else if (vsComputer) ComputerTurn();
                else PlayerTurn(p2Hand, ref p2Life, ref p1Life);

                isPlayer1Turn = !isPlayer1Turn; 
            }
            ShowResult();  // Show results
        }

        /* Player Turn Logic */
        static void PlayerTurn(List<Card> hand, ref int currentLife, ref int opponentLife)
        {
            Console.WriteLine($"\nCurrent required suit: {GetCurrentSuitDisplay()}");
            Console.WriteLine("Your hand:");
            DisplayHand(hand);

            int choice = GetChoice(1, hand.Count) - 1;
            Card played = hand[choice];
            hand.RemoveAt(choice);

            ProcessEffects(played, ref opponentLife, ref currentLife);
            UpdateGameState(played, ref currentLife);
        }

        /* Computer Turn Logic */
        static void ComputerTurn()
        {
            Card played = p2Hand[new Random().Next(p2Hand.Count)];
            p2Hand.Remove(played);
            Console.WriteLine($"\nComputer plays: {played}");
            ProcessEffects(played, ref p1Life, ref p2Life);
            UpdateGameState(played, ref p2Life);
        }

        /* Card Effect Processing System */
        static void ProcessEffects(Card card, ref int opponentLife, ref int currentLife)
        {
            if (card.IsJoker) return;

            // Apply effects based on suit and rank
            switch (card.Suit)
            {
                case Suit.Hearts when card.Rank == "A":
                    opponentLife -= 2;  // Hearts A: Opponent -2HP
                    Console.WriteLine("! Opponent loses 2 HP!");
                    break;

                case Suit.Diamonds when card.Rank == "A":
                    blockDraw = true;   // Diamonds A: Block draw
                    Console.WriteLine("! Opponent's draw blocked!");
                    break;

                case Suit.Spades when card.Rank == "A":
                    restrictNumbers = true;  // Spades A: Restrict numbers
                    Console.WriteLine("! Opponent restricted to low cards!");
                    break;

                case Suit.Clubs when card.Rank == "A":
                    currentLife += 2;  // Clubs A: Self +2HP
                    Console.WriteLine("! Player gains 2 HP!");
                    break;
            }
        }

        static void UpdateGameState(Card played, ref int life)
        {
            bool valid = played.IsJoker || played.Suit == currentSuit;
            if (!valid)
            {
                life -= 2;  //  Wrong suit penalty
                Console.WriteLine("! Wrong suit, lost 2 HP!");
                currentSuit = played.Suit;  // Update required suit
            }
            else if (!played.IsJoker)
            {
                currentSuit = played.Suit;
            }
        }

        /* Draw Phase Handling */
        static void DrawPhase()
        {
            RefillHand(p1Hand);
            if (!blockDraw) RefillHand(p2Hand);
            blockDraw = false;  
        }

        /* Hand Refill Logic */
        static void RefillHand(List<Card> hand)
        {
            while (hand.Count < 3 && deck.Count > 0)
                hand.Add(deck.Draw());
        }

        /* Game Status Display */
        static void ShowStatus()
        {
            Console.WriteLine($"\nPlayer 1 HP: {p1Life}  |  Player 2 HP: {p2Life}");
            Console.WriteLine($"Cards remaining: {deck.Count}");
            Console.WriteLine($"Current turn: {(isPlayer1Turn ? "Player 1" : vsComputer ? "Computer" : "Player 2")}");
        }

        /* Win/Lose Determination */
        static bool GameOver()
        {
            if (p1Life <= 0 || p2Life <= 0) return true;
            if (deck.Count == 0 && p1Hand.Count == 0 && p2Hand.Count == 0)
                return p1Life != p2Life;
            return false;
        }

        /* Result Display */
        static void ShowResult()
        {
            Console.WriteLine("\n=== GAME OVER ===");
            if (p1Life <= 0) Console.WriteLine("Player 2 Wins!");
            else if (p2Life <= 0) Console.WriteLine("Player 1 Wins!");
            else Console.WriteLine(p1Life > p2Life ? "Player 1 Wins!" : "Player 2 Wins!");
        }

        static void DisplayHand(List<Card> hand)
        {
            Console.WriteLine(string.Join("  ", 
                hand.Select((c, i) => $"[{i + 1}]{c}")));
        }

        /* Player Input */
        static int GetChoice(int min, int max)
        {
            int choice;
            do
            {
                Console.Write($"Select ({min}-{max}): ");
            } while (!int.TryParse(Console.ReadLine(), out choice) || choice < min || choice > max);
            return choice;
        }

        /* Current Suit Display Formatter */
        static string GetCurrentSuitDisplay()
        {
            return currentSuit switch
            {
                Suit.Hearts => "H",
                Suit.Spades => "S",
                Suit.Diamonds => "D",
                Suit.Clubs => "C",
                _ => "ANY"
            };
        }
    }
}