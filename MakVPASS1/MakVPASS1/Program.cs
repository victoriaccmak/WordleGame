/*Author: Victoria Mak
 * File Name: Program.cs
 * Project Name: MakVPASS1
 * Creation Date: February 15, 2023
 * Modified Date: February 26, 2023
 * Description: Play the game, Wordle, where you attempt to guess a 5-letter word within 6 tries with color-coded hints given.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace MakVPASS1
{
    class Program
    {
        //Store the window width and height
        const int WINDOW_WIDTH = 46;
        const int WINDOW_HEIGHT = 30;

        //Store the stream reader and writer for reading and writing to the dictionary, gameboard, and statistics files
        static StreamReader inFile;
        static StreamWriter outFile;

        //Store the constant game states
        const byte MENU = 0;
        const byte PLAY = 1;
        const byte INSTRUCTIONS = 2;
        const byte STATS = 3;
        const byte EXITED = 4;

        //Store the indexes that shows the information pertaining to the rows and columns of the game board when reading in the gameboard file
        const int ROW_INDEX = 0;
        const int COL_INDEX = 1;

        //Store the max number of rounds
        static int MAX_ROUNDS = 6;

        //Store the valid word length
        const int WORD_LENGTH = 5;

        //Store the constant grid colour
        const ConsoleColor GRID_COLOUR = ConsoleColor.White;

        //Store the numbers representing the colours
        const int CORRECT = 3;
        const int CONTAINS = 2;
        const int WRONG = 1;
        const int UNUSED = 0;

        //Store the number of stats
        const int NUM_OF_STATS = 4;
        
        //Store the spacing of the horizontal statistics and the buffer from the left side of the screen for the stats
        const int STAT_SPACING = 8;
        const int STATS_LEFT_BUFFER = (WINDOW_WIDTH - STAT_SPACING * NUM_OF_STATS) / 2;

        //Store the full progress bar width
        const int FULL_BAR_WIDTH = WINDOW_WIDTH - 6;

        //Store the random number generator
        static Random rng = new Random();

        //Store the game board center spacing vertically and horizontally, top and left buffer
        static int gridVertSpacing;
        static int gridHorSpacing;
        static int gridTopBuffer;
        static int gridLeftBuffer;

        //Store the lists for the answer words and for the extra words
        static List<string> answers = new List<string>();
        static List<string> extraWords = new List<string>();

        //Store the game board characters and the number of rows and columns
        static char[,] gameBoardChars;
        static int numRows;
        static int numCols;

        //Store the letter options
        static char[] letterOptions = new char[] { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z' };

        //Store the meaning of the colors for the feedback of letter guesses
        static ConsoleColor[] codedColors = { ConsoleColor.White, ConsoleColor.DarkGray, ConsoleColor.DarkYellow, ConsoleColor.Green };

        //Store the round number
        static int round = 1;

        //Store the number of games played, win percentage, the current and max win streak, the guess distribution, and the greatest frequency of the distributions
        static int gamesPlayed = 0;
        static int gamesWon = 0;
        static int curWinStreak = 0;
        static int maxWinStreak = 0;
        static int greatestDistFrequency = 1;

        //Store the guess distribution frequencies
        static int[] guessDist = new int[MAX_ROUNDS];

        //Store the answer
        static string answer = "";

        //Store whether the round is over
        static bool gameIsOver = false;

        //Store all guesses
        static string[] allGuesses = new string[MAX_ROUNDS];

        //Store the colour of the letters in the guesses and the colours of the alphabet
        static int[,] statusOfGuesses = new int[MAX_ROUNDS, WORD_LENGTH];
        static int[] letterStatuses = new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        
        static void Main(string[] args)
        {
            //Store the user's choice
            string userChoice;

            //Store the user's guess and the feedback for the guess
            string curGuess;
            string feedback = "";

            //Store the game state
            byte gameState = MENU;

            //Set the windows size
            Console.SetWindowSize(WINDOW_WIDTH, WINDOW_HEIGHT);

            //Load the dictionaries and store the words into each list
            answers = LoadDictionary("WordleAnswers.txt");
            extraWords = LoadDictionary("WordleExtras.txt");

            //Load the stats and gameboard
            LoadStats();
            SetUpGameBoard();

            //Continue the program as long as the game state is not exited
            while (gameState != EXITED)
            {
                //Display and update the console depending on the game state
                switch (gameState)
                {
                    case MENU:
                        //Clear the screen
                        Console.Clear();

                        //Display the title and menu
                        DisplayTitle();
                        DisplayMenu();

                        //Retrieve the user's choice
                        Console.Write("Enter option: ");
                        userChoice = Console.ReadLine().Trim();

                        //Switch the game state according to the user's choice
                        switch(userChoice)
                        {
                            case "1":
                                //Setup a new game and change the game state
                                gameState = PLAY;
                                SetupNewGame();
                                break;

                            case "2":
                                //Change the game state to the instructions
                                gameState = INSTRUCTIONS;
                                break;

                            case "3":
                                //Change the game state to exit and display the exit message
                                gameState = EXITED;
                                DisplayContinueMsg("You have exited the program.", "Press ENTER to exit.", ConsoleColor.Yellow);
                                break;

                            default:
                                //Display an error message on the user's choice
                                Console.BackgroundColor = ConsoleColor.Red;
                                DisplayContinueMsg("Enter a valid option.", "Press ENTER to continue.", ConsoleColor.Red);
                                Console.ReadLine();
                                break;
                        }

                        break;

                    case PLAY:
                        //Display the gameplay as long as the game state is in play
                        while(gameState == PLAY)
                        {
                            //Clear the console and display the title, alphabet and the gameboard with the user's guesses
                            Console.Clear();
                            DisplayTitle();
                            DisplayAlphabet();
                            DisplayGameBoard();

                            //Retrieve and evaluate a new guess as long if the game isn't over
                            if (!gameIsOver)
                            {
                                //Display the feedback if the previous guess is invalid
                                if (feedback.Length > 0)
                                {
                                    //Display the message informing the user about the invalid guess
                                    Console.BackgroundColor = ConsoleColor.Red;
                                    Console.WriteLine(CenterText("Invalid guess:", WINDOW_WIDTH));
                                    Console.WriteLine(CenterText(feedback, WINDOW_WIDTH));
                                    Console.ResetColor();
                                }

                                //Retrieve the guess from the user
                                Console.Write("Enter Guess " + round + ": ");
                                curGuess = Console.ReadLine().Trim();

                                //Evaluate the word or reject the word depending on whether the word is valid
                                if (curGuess.Length != WORD_LENGTH)
                                {
                                    //Set the feedback to tell the user to guess a word 5 letters long
                                    feedback = "Guess needs to be 5 letters long.";
                                }
                                else if (!answers.Contains(curGuess.ToLower()) && !extraWords.Contains(curGuess.ToLower()))
                                {
                                    //Set the feedback to tell the user to guess a real word
                                    feedback = "Not a real word.";
                                }
                                else
                                {
                                    //Set the feedback as empty
                                    feedback = "";

                                    //Convert the guess to upper case
                                    allGuesses[round - 1] = curGuess.ToUpper();


                                    //Set all letter colors in the guess as the correct color and end the game
                                    if (allGuesses[round - 1].Equals(answer))
                                    {
                                        //Set all letter colors in the round as green
                                        for (int i = 0; i < WORD_LENGTH; i++)
                                        {
                                            //Set each letter guessed in that round and letters in the alphabet as the correct colour
                                            statusOfGuesses[round - 1, i] = CORRECT;
                                            SetLetterStatus(Convert.ToInt32(allGuesses[round - 1][i]) - Convert.ToInt32('A'), CORRECT);
                                        }

                                        //Set the status of the game as over 
                                        gameIsOver = true;
                                    }
                                    else
                                    {
                                        //Evaluate the word
                                        EvaluateWord();

                                        //End game or increase the round depending on whether there is another round
                                        if (round >= MAX_ROUNDS)
                                        {
                                            //Set the game as over
                                            gameIsOver = true;
                                        }
                                        else
                                        {
                                            //Increase the round by 1
                                            round++;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                //Set the winning or losing message and set the stats depending on the win or loss
                                if (allGuesses[round - 1].Equals(answer))
                                {
                                    //Increase the games won, the win streak, and the guess distribution at the number of guesses it took
                                    gamesWon++;
                                    curWinStreak++;
                                    guessDist[round - 1]++;

                                    //Change the maximum win streak if it is less than the current win streak
                                    if (maxWinStreak < curWinStreak)
                                    {
                                        //Set the maximum win streak as the current win streak
                                        maxWinStreak = curWinStreak;
                                    }

                                    //Set the greatest distribution frequency if it is less than the frequency at the number of guesses taken at the previous round
                                    if (greatestDistFrequency < guessDist[round - 1])
                                    {
                                        //Set the greatest distribution frequency
                                        greatestDistFrequency = guessDist[round - 1];
                                    }

                                    //Display the winning message in a green color
                                    Console.BackgroundColor = ConsoleColor.DarkGreen;
                                    DisplayContinueMsg("You guessed " + answer + " in " + round + " guesses!", "Press ENTER to see stats.", ConsoleColor.DarkGreen);
                                }
                                else
                                {
                                    //Set the current win streak to 0
                                    curWinStreak = 0;

                                    //Display the losing message in a red color
                                    Console.BackgroundColor = ConsoleColor.Red;
                                    DisplayContinueMsg("You lost! The answer was " + answer + ".", "Press ENTER to see stats.", ConsoleColor.Red);
                                }

                                //Wait for the user to press enter
                                Console.ReadLine();

                                //Increase the games played and then save the stats
                                gamesPlayed++;
                                SaveStats();

                                //Change the game state to show the stats
                                gameState = STATS;
                            }
                        }
                        break;

                    case INSTRUCTIONS:
                        //Clear the screen
                        Console.Clear();

                        //Display the instructions
                        DisplayInstructions();

                        //Display the message to the user to press enter to go back to the menu
                        DisplayContinueMsg("Good luck!", "Press ENTER to go back to the menu.", ConsoleColor.Yellow);
                        Console.ReadLine();

                        //Change the game state to the menu
                        gameState = MENU;
                        break;

                    case STATS:
                        //Clear the screen
                        Console.Clear();

                        //Display statistics and the stat menu options
                        DisplayStats();
                        DisplayStatMenuOptions();

                        //Retrieve the user's choice
                        Console.Write("Enter option: ");
                        userChoice = Console.ReadLine().Trim();

                        //Change the state to the pregame, reset stats, or exit the game depending on the user's choice
                        switch (userChoice)
                        {
                            case "1":
                                //Set up a new game and change the game state
                                gameState = PLAY;
                                SetupNewGame();
                                break;

                            case "2":
                                //Set all the stats to 0
                                gamesPlayed = 0;
                                gamesWon = 0;
                                curWinStreak = 0;
                                maxWinStreak = 0;
                                greatestDistFrequency = 1;
                                
                                //Set all the guess distribution values to 0
                                for (int i = 0; i < MAX_ROUNDS; i++)
                                {
                                    //Set each distribution to 0
                                    guessDist[i] = 0;
                                }

                                //Save the cleared statistics
                                SaveStats();

                                //Display the message to the user that the stats have been reset
                                DisplayContinueMsg("All stats reset.", "Press ENTER to continue.", ConsoleColor.Yellow);
                                Console.ReadLine();
                                break;

                            case "3":
                                //Go back to the menu
                                gameState = MENU;
                                break;

                            default:
                                //Display an error message on the user's choice
                                Console.BackgroundColor = ConsoleColor.Red;
                                DisplayContinueMsg("Enter a valid option.", "Press ENTER to continue.", ConsoleColor.Red);
                                Console.ReadLine();
                                break;
                        }
                        break;
                }
            }

            //Wait for the user to press enter to exit the program
            Console.ReadLine();
        }

        //Pre: filePath is the name of a dictionary text file, including its extension
        //Post: returns a list of 5-letter words retrieved from the dictionary file
        //Desc: load the words from the given dictionary file and store it in a list to be returned
        private static List<string> LoadDictionary(string filePath)
        {
            //Store the words retrieved from the dictionary file
            List<string> words = new List<string>();

            try
            {
                //Open the file of the dictionary
                inFile = File.OpenText(filePath);

                //Add a new word into the list of words from the dictionary as long as the end of the file has not been reached
                while(!inFile.EndOfStream)
                {
                    //Add the next word into the list
                    words.Add(inFile.ReadLine());
                }
            }
            catch (FileNotFoundException fnf)
            {
                //Display the error message on the file not found
                Console.BackgroundColor = ConsoleColor.Red;
                Console.WriteLine(CenterText("ERROR: " + filePath + ", which stores", WINDOW_WIDTH));
                Console.WriteLine(CenterText("words from the dictionary, was not found.", WINDOW_WIDTH));
                Console.ResetColor();
            }
            catch (Exception e)
            {
                //Display the general error message
                Console.BackgroundColor = ConsoleColor.Red;
                Console.WriteLine(CenterText("There was an error loading the dictionary,", WINDOW_WIDTH));
                Console.WriteLine(CenterText("specifically " + filePath, WINDOW_WIDTH));
                Console.ResetColor();
            }
            finally
            {
                //Close the file if it hasn't been opened
                if (inFile != null)
                {
                    //Close the dictionary file
                    inFile.Close();
                }
            }

            //Return the words
            return words;
        }

        //Pre: none
        //Post: none
        //Desc: load the game board by retrieving its dimensions and storing the characters that make up the board in a 2D array
        private static void SetUpGameBoard()
        {
            //Store the array of data for the values that are separated by commas in lines
            string[] data;

            try
            {
                //Open the file
                inFile = File.OpenText("WordleGrid.txt");

                //Set the array of data as the values split with commas in a line
                data = inFile.ReadLine().Split(',');

                //Set the number of rows and number of columns to read in the game board
                numRows = Convert.ToInt32(data[ROW_INDEX]);
                numCols = Convert.ToInt32(data[COL_INDEX]);

                //Set the array of data as the values split with commas in a line
                data = inFile.ReadLine().Split(',');

                //Set the top and left buffer for the first box center in the game board
                gridTopBuffer = Convert.ToInt32(data[ROW_INDEX]);
                gridLeftBuffer = Convert.ToInt32(data[COL_INDEX]);

                //Set the array of data as the values split with commas in a line
                data = inFile.ReadLine().Split(',');

                //Set the vertical and horizontal spacing between the centers of the boxes in the game board
                gridVertSpacing = Convert.ToInt32(data[ROW_INDEX]);
                gridHorSpacing = Convert.ToInt32(data[COL_INDEX]);

                //Set the size of the 2D array holding the game board characters with its number of rows and columns
                gameBoardChars = new char[numRows, numCols];

                //Read each line of game board characters
                for (int i = 0; i < numRows; i++)
                {
                    //Set the next line of data
                    data[0] = inFile.ReadLine();

                    //Set the game board characters in the row as each character in the line of input
                    for (int j = 0; j < numCols; j++)
                    {
                        //Set the game board character at each position
                        gameBoardChars[i, j] = data[0][j];
                    }
                }
            }
            catch (FileNotFoundException fnf)
            {
                //Display the error message on the file not found
                Console.BackgroundColor = ConsoleColor.Red;
                Console.WriteLine(CenterText("ERROR: The file storing the", WINDOW_WIDTH));
                Console.WriteLine(CenterText("game board was not found.", WINDOW_WIDTH));
                Console.ResetColor();
            }
            catch (FormatException fe)
            {
                //Display the error message on the file's formatting error
                Console.BackgroundColor = ConsoleColor.Red;
                Console.WriteLine(CenterText("ERROR: There is a formatting error trying", WINDOW_WIDTH));
                Console.WriteLine(CenterText("to convert invalid data from the file", WINDOW_WIDTH));
                Console.WriteLine(CenterText("storing the game board.", WINDOW_WIDTH));
                Console.ResetColor();
            }
            catch (EndOfStreamException eos)
            {
                //Display the error message on how the program tried to read past the file
                Console.BackgroundColor = ConsoleColor.Red;
                Console.WriteLine(CenterText("ERROR: The program tried to read past", WINDOW_WIDTH));
                Console.WriteLine(CenterText("the file storing the statistics.", WINDOW_WIDTH));
                Console.ResetColor();
            }
            catch (IndexOutOfRangeException ore)
            {
                //Display the error message on how the program tried to read past an array or string
                Console.BackgroundColor = ConsoleColor.Red;
                Console.WriteLine(CenterText("ERROR: The program tried to read past", WINDOW_WIDTH));
                Console.WriteLine(CenterText("an array in the file storing the game board.", WINDOW_WIDTH));
                Console.ResetColor();
            }
            catch (Exception e)
            {
                //Display the general error message
                Console.BackgroundColor = ConsoleColor.Red;
                Console.WriteLine(CenterText("There was an error loading the game board.", WINDOW_WIDTH));
                Console.ResetColor();
            }
            finally
            {
                //Close the file if it had been opened
                if (inFile != null)
                {
                    //Close the file
                    inFile.Close();
                }
            }
        }

        //Pre: none
        //Post: none
        //Desc: load the statistics and store them into their corresponding variables so that they can be displayed to the user
        private static void LoadStats()
        {
            //Store the array of data that would be retrieved in a line from the file
            string[] data;

            try
            {
                //Open the file containing the statistics
                inFile = File.OpenText("Stats.txt");

                //Retrieve the games played, won, win percentage, current and maximum win streak, and the greatest frequency of the guess distribution
                gamesPlayed = Convert.ToInt32(inFile.ReadLine());
                gamesWon = Convert.ToInt32(inFile.ReadLine());
                curWinStreak = Convert.ToInt32(inFile.ReadLine());
                maxWinStreak = Convert.ToInt32(inFile.ReadLine());
                greatestDistFrequency = Convert.ToInt32(inFile.ReadLine());
                
                //Read the distribution frequency by splitting the values with a comma
                data = inFile.ReadLine().Split(',');

                //Set the guess distribution as the values from the array of data values
                for (int i = 0; i < MAX_ROUNDS; i++)
                {
                    //Set the guess distribution for each guess number as the data value
                    guessDist[i] = Convert.ToInt32(data[i]);
                }
            }
            catch (FileNotFoundException fnf)
            {
                //Display the error message on the file not found
                Console.BackgroundColor = ConsoleColor.Red;
                Console.WriteLine(CenterText("ERROR: The file storing the", WINDOW_WIDTH));
                Console.WriteLine(CenterText("statistics was not found.", WINDOW_WIDTH));
                Console.ResetColor();
            }
            catch (FormatException fe)
            {
                //Display the error message on the file's formatting error
                Console.BackgroundColor = ConsoleColor.Red;
                Console.WriteLine(CenterText("ERROR: There is a formatting error trying to", WINDOW_WIDTH));
                Console.WriteLine(CenterText("convert invalid data from the statistics file.", WINDOW_WIDTH));
                Console.ResetColor();
            }
            catch (EndOfStreamException eos)
            {
                //Display the error message on how the program tried to read past the file
                Console.BackgroundColor = ConsoleColor.Red;
                Console.WriteLine(CenterText("ERROR: The program tried to read past", WINDOW_WIDTH));
                Console.WriteLine(CenterText("the file storing the statistics.", WINDOW_WIDTH));
                Console.ResetColor();
            }
            catch (IndexOutOfRangeException ore)
            {
                //Display the error message on how the program tried to read past the index of an array or string
                Console.BackgroundColor = ConsoleColor.Red;
                Console.WriteLine(CenterText("ERROR: The program tried to read past an array", WINDOW_WIDTH));
                Console.WriteLine(CenterText("or string in the file storing the statistics.", WINDOW_WIDTH));
                Console.ResetColor();
            }
            catch (Exception e)
            {
                //Display the general error message
                Console.BackgroundColor = ConsoleColor.Red;
                Console.WriteLine(CenterText("There was an error loading the statistics file.", WINDOW_WIDTH));
                Console.ResetColor();
            }
            finally
            {
                //Close the file if it was opened before
                if (inFile != null)
                {
                    //Close the statistics file
                    inFile.Close();
                }
            }
        }

        //Pre: none
        //Post: none
        //Desc: write the statistics into the stats text file so that the values are saved
        private static void SaveStats()
        {
            try
            {
                //Open the statistic file
                outFile = File.CreateText("stats.txt");

                //Write the games played, won, win percentage, current and maximum win streak, and the greatest frequency in the guess distribution
                outFile.WriteLine(gamesPlayed);
                outFile.WriteLine(gamesWon);
                outFile.WriteLine(curWinStreak);
                outFile.WriteLine(maxWinStreak);
                outFile.WriteLine(greatestDistFrequency);

                //Write each frequency for each number of rounds in the guess distribution
                for (int i = 0; i < MAX_ROUNDS; i++)
                {
                    //Write the frequency for the rounds taken to guess the word
                    outFile.Write(guessDist[i]);

                    //Write a comma to separate the frequencies for each number of rounds
                    if (i != MAX_ROUNDS - 1)
                    {
                        //Write a comma
                        outFile.Write(",");
                    }
                }
            }
            catch (FileNotFoundException fnf)
            {
                //Display the error message on the file not found
                Console.BackgroundColor = ConsoleColor.Red;
                Console.WriteLine(CenterText("ERROR: The file storing the", WINDOW_WIDTH));
                Console.WriteLine(CenterText("statistics was not found.", WINDOW_WIDTH));
                Console.ResetColor();
            }
            catch (EndOfStreamException eos)
            {
                //Display the error message on how the program tried to read past the file
                Console.BackgroundColor = ConsoleColor.Red;
                Console.WriteLine(CenterText("ERROR: The program tried to read past", WINDOW_WIDTH));
                Console.WriteLine(CenterText("the file storing the statistics.", WINDOW_WIDTH));
                Console.ResetColor();
            }
            catch (IndexOutOfRangeException ore)
            {
                //Display the error message on how the program tried to read past the file
                Console.BackgroundColor = ConsoleColor.Red;
                Console.WriteLine(CenterText("ERROR: The program tried to write past an array", WINDOW_WIDTH));
                Console.ResetColor();
            }
            catch (Exception e)
            {
                //Display the general error message
                Console.BackgroundColor = ConsoleColor.Red;
                Console.WriteLine(CenterText("There was an error saving the statistics.", WINDOW_WIDTH));
                Console.ResetColor();
            }
            finally
            {
                //Close the file if it had been opened
                if (outFile != null)
                {
                    //Close the file
                    outFile.Close();
                }
            }
        }

        //Pre: none
        //Post: none
        //Desc: displays the welcoming title of the game in blue
        private static void DisplayTitle()
        {
            //Display the title in blue
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine(CenterText("Welcome to Wordle Educational Edition!", WINDOW_WIDTH));
            Console.WriteLine();
        }

        //Pre: none
        //Post: none
        //Desc: display the choices of the main menu
        private static void DisplayMenu()
        {
            //Display the message telling the user to select an option
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(CenterText("Select an option:", WINDOW_WIDTH));

            //Display the options
            Console.WriteLine(" 1. Play");
            Console.WriteLine(" 2. Instructions");
            Console.WriteLine(" 3. Quit");
        }

        //Pre: none
        //Post: none
        //Desc: display the choices after the stats are displayed
        private static void DisplayStatMenuOptions()
        {
            //Display the message telling the user to select an option
            Console.WriteLine(CenterText("Select an option:", WINDOW_WIDTH));

            //Display the menu options and retrieve the user's choice
            Console.WriteLine(" 1. Play Again");
            Console.WriteLine(" 2. Reset Stats");
            Console.WriteLine(" 3. Menu");
        }

        //Pre: msg1 and msg2 are phrases with a length less than the window's width and highlightColor is a ConsoleColor 
        //Post: none
        //Desc: displays both messages centered in the window; the line that msg1 is displayed on is highlighted with the specified color while msg2 is unhighlighted and in white
        private static void DisplayContinueMsg(string msg1, string msg2, ConsoleColor highlightColor)
        {
            //Display the first message in the highlighted colour
            Console.BackgroundColor = highlightColor;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.WriteLine(CenterText(msg1, WINDOW_WIDTH));

            //Display the second message in the normal color
            Console.ResetColor();
            Console.WriteLine(CenterText(msg2, WINDOW_WIDTH));
        }

        //Pre: none
        //Post: none
        //Desc: display all the letter choices in their specified colors
        private static void DisplayAlphabet()
        {
            //Center the alphabet on the screen
            Console.Write("".PadLeft((WINDOW_WIDTH - letterOptions.Length) / 2));

            //Display the letter options in the colour showing their status
            for (int i = 0; i < letterOptions.Length; i++)
            {
                //Set the letter colour
                Console.ForegroundColor = codedColors[letterStatuses[i]];
                Console.Write(letterOptions[i]);
            }
        }

        //Pre: word is a 5-letter word, status is an integer from 1 to 3, and affectedBox is an integer from 1 to 5
        //Post: none
        //Desc: displays only 1 row of the gameboard with the 5 letters in each box; one of the letters are colored according to the status at the specified box column
        private static void DisplayExample(string word, int status, int affectedBox)
        {
            //Store the current box index where the letter is being added
            int boxIndex = 0;

            //Display each row of the game board characters
            for (int i = 0; i < gameBoardChars.GetLength(0); i++)
            {
                //If the current row being displayed is the 3rd row, set the row to display as the last row of characters in the game board
                if (i == 2)
                {
                    //Set the current row to display as the last row
                    i = gameBoardChars.GetLength(0) - 1;
                }

                //Display each column of the game board characters
                for (int j = 0; j < numCols; j++)
                {
                    //If the row of characters displays the letters, display the word's letters in the center of each box
                    if (i == gridTopBuffer && (j - gridLeftBuffer) % gridHorSpacing == 0)
                    {
                        //Display the letter in its example colour if the box is affected
                        if (boxIndex == affectedBox - 1)
                        {
                            //Change the colour, display the letter, and reset the colour
                            Console.ForegroundColor = codedColors[status];
                            Console.Write(word[boxIndex]);
                            Console.ForegroundColor = GRID_COLOUR;
                        }
                        else
                        {
                            //Write the word in the same color as the grid
                            Console.Write(word[boxIndex]);
                        }

                        //Increase the box index to the next box
                        boxIndex++;
                    }
                    else
                    {
                        //Write the character that makes up the game board
                        Console.Write(gameBoardChars[i, j]);
                    }
                }

                //Write to the next line to display the next row underneath
                Console.WriteLine();
            }
        }

        //Pre: none
        //Post: none
        //Desc: display the instructions page, including the title, rules, and examples
        private static void DisplayInstructions()
        {
            //Display the instructions title
            Console.WriteLine(CenterText("HOW TO PLAY", WINDOW_WIDTH));
            Console.WriteLine("".PadLeft(WINDOW_WIDTH, '─'));

            //Display the rules
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Guess the Wordle in 6 tries.");
            Console.WriteLine(" ■ Each guess must be a valid 5-letter word.");
            Console.WriteLine(" ■ The color of the tiles will change to \n  show how close your guess was to the word.");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("".PadLeft(WINDOW_WIDTH, '─'));

            //Display the examples title
            Console.WriteLine("Examples\n");

            //Display the example with the correct letter position
            DisplayExample("GRAIN", CORRECT, 2);
            Console.WriteLine("R is in the word and in the correct spot.\n");

            //Display the example with the correct letter but wrong position
            DisplayExample("ARBOR", CONTAINS, 1);
            Console.WriteLine("A is in the word but in the wrong spot.\n");

            //Display the example with a wrong letter 
            DisplayExample("SPACE", WRONG, 5);
            Console.WriteLine("E is not in the word in any spot.\n");
        }

        //Pre: none
        //Post: none
        //Desc: display the gameboard with the characters of each round's guesses in each box of the grid with their coded colors
        private static void DisplayGameBoard()
        {
            //Store the current indexes for the row and column of the box in the grid that has a letter being added
            int boxRowIndex = 0;
            int boxColIndex = 0;

            //Change the color to the grid color and draw the grid on a line below the alphabet
            Console.ForegroundColor = GRID_COLOUR;
            Console.WriteLine();

            //Display each row in the grid
            for (int i = 0; i < numRows; i++)
            {
                //Center the grid
                Console.Write("".PadLeft((WINDOW_WIDTH - numCols) / 2));

                //Display each column in the grid
                for (int j = 0; j < numCols; j++)
                {
                    //Display each letter in the grid
                    if ((i - gridTopBuffer) % gridVertSpacing == 0 && (j - gridLeftBuffer) % gridHorSpacing == 0 && allGuesses[boxRowIndex] != null)
                    {
                        //Display letters with their colours
                        Console.ForegroundColor = codedColors[statusOfGuesses[boxRowIndex, boxColIndex]];
                        Console.Write(allGuesses[boxRowIndex][boxColIndex]);

                        //Change back the foreground colour to white for the grid lines
                        Console.ForegroundColor = GRID_COLOUR;

                        //Increase the box column's index
                        boxColIndex++;

                        //If the box column index is greater than the word's last index, move down 1 row and change the column to 0
                        if (boxColIndex > WORD_LENGTH - 1)
                        {
                            //Set the box column index to 0 and increase the row index by 1
                            boxColIndex = 0;
                            boxRowIndex++;
                        }
                    }
                    else
                    {
                        //Display the grid
                        Console.Write(gameBoardChars[i, j]);
                    }
                }

                //Enter a new line to draw the next row of the game board 
                Console.WriteLine();
            }
        }

        //Pre: none
        //Post: none
        //Desc: display the statistics page, including the title, statistics, and the guess distribution bars
        private static void DisplayStats()
        {
            //Store the win percentage
            int winPercentage;

            //Calculate the win percentage if there were games played or set it as 0
            if (gamesPlayed > 0)
            {
                //Calculate the win percentage as the percentage of games won divided by the number of games played
                winPercentage = (int)Math.Round((double)gamesWon / gamesPlayed * 100);
            }
            else
            {
                //Set the win percentage to 0
                winPercentage = 0;
            }

            //Write the statistics title
            Console.WriteLine(CenterText("STATISTICS", WINDOW_WIDTH));
            Console.WriteLine();

            //Display the stats in yellow
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("".PadLeft(STATS_LEFT_BUFFER) + CenterText(Convert.ToString(gamesPlayed), STAT_SPACING));
            Console.Write(CenterText(Convert.ToString(winPercentage), STAT_SPACING));
            Console.Write(CenterText(Convert.ToString(curWinStreak), STAT_SPACING));
            Console.Write(CenterText(Convert.ToString(maxWinStreak), STAT_SPACING));
            Console.WriteLine();

            //Display the first word in the label of the statistics
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.Write("".PadLeft(STATS_LEFT_BUFFER) + CenterText("Played", STAT_SPACING));
            Console.Write(CenterText("Win %", STAT_SPACING));
            Console.Write(CenterText("Win", STAT_SPACING));
            Console.Write(CenterText("Max", STAT_SPACING));

            //Display the second word of the win streaks of the statistics
            Console.WriteLine();
            Console.Write("".PadLeft(STATS_LEFT_BUFFER + STAT_SPACING * 2) + CenterText("Streak", STAT_SPACING));
            Console.Write(CenterText("Streak", STAT_SPACING));

            //Display the guess distribution title with underlines to split the statistic sections
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("".PadLeft(WINDOW_WIDTH, '═'));
            Console.WriteLine("Guess Distribution".PadLeft((WINDOW_WIDTH + "Guess Distribution".Length) / 2));
            Console.WriteLine("".PadLeft(WINDOW_WIDTH, '─'));

            //Display the guess distribution bars 
            for (int i = 0; i < MAX_ROUNDS; i++)
            {
                //Display the label of the guess distributions in white
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write(" " + (i + 1));

                //If the game was previously won, display the distribution bar as green at the number of guesses taken in the previous round
                if (round == i + 1 && curWinStreak >= 1)
                {
                    //Change the color to dark green
                    Console.BackgroundColor = ConsoleColor.DarkGreen;
                }
                else
                {
                    //Change the color to dark gray
                    Console.BackgroundColor = ConsoleColor.DarkGray;
                }

                //Draw the bar of the guess distribution with its width relative to the greatest frequency of the guess distributions
                Console.WriteLine((" " + guessDist[i] + " ").PadLeft((int)Math.Round(FULL_BAR_WIDTH * guessDist[i] / (double)greatestDistFrequency) + 3, ' '));
                Console.ResetColor();

                //Draw the lines separating the bars
                Console.WriteLine("".PadLeft(WINDOW_WIDTH, '─'));
            }
        }

        //Pre: letterIndex is an integer from 0 to 25 while status is an integer from 0 to 3
        //Post: none
        //Desc: set the given status of the letter from the specified index of the alphabet if its status was less than the given status
        private static void SetLetterStatus(int letterIndex, int status)
        {
            //Set the letter status in the alphabet if it had a lower status
            if (letterStatuses[letterIndex] < status)
            {
                //Set the status of the letter
                letterStatuses[letterIndex] = status;
            }
        }

        //Pre: none
        //Post: none
        //Desc: set up a new game by reseting the round, setting the game as not over,clearing guesses and statuses of letters, and randomizing an answer
        private static void SetupNewGame()
        {
            //Set the round to 1 and set the game to not being over
            round = 1;
            gameIsOver = false;

            //Clear the guesses from the previous round
            allGuesses = new string[MAX_ROUNDS];

            //Reset the colours of the letters chosen from the previous round
            statusOfGuesses = new int[MAX_ROUNDS, WORD_LENGTH];

            //Reset all letter statuses to being unused
            for (int i = 0; i < letterStatuses.Length; i++)
            {
                //Change the status to unused as long if the letter had been used in the previous game
                if (letterStatuses[i] > UNUSED)
                {
                    //Set tne letter status to being unused
                    letterStatuses[i] = UNUSED;
                }
            }

            //Randomize a word in upper case to be the answer to the round 
            answer = answers[rng.Next(0, answers.Count)].ToUpper();
        }

        //Pre: msg is any phrase and maxWidth is an integer greater or equal to the length of the phrase
        //Post: returns the centered phrase with an equal number of spaces on its left or right (or one more space on the right than left)
        //Desc: centers the message in the specified width by padding the left and right of the string with spaces
        private static string CenterText(string msg, int maxWidth)
        {
            //Return the message with an equal number of spaces on the left or right (or 1 space more on the right than left)
            return msg.PadLeft((maxWidth + msg.Length) / 2).PadRight(maxWidth);
        }

        //Pre: none
        //Post: none
        //Desc: evaluates the guess by checking for correct letter placements and letters that are contained in the answer, then sets the statuses of each letter in the guess and alphabet
        private static void EvaluateWord()
        {
            //Set the temporary answer as the answer for checking the correctness of the letters
            string tempAnswer = answer;

            //In each position in the guess, compare the letter of the guess to the answer to check for a correct position
            for (int i = 0; i < WORD_LENGTH; i++)
            {
                //Compare each letter in each position of the word to set the status for the letters that are correct 
                if (allGuesses[round - 1][i] == tempAnswer[i])
                {
                    //Set the letter as correct and substitute a space in the temporary answer for that letter so that the letter cannot be compared again
                    statusOfGuesses[round - 1, i] = CORRECT;
                    tempAnswer = tempAnswer.Substring(0, i) + " " + tempAnswer.Substring(i + 1);

                    //Set the letter status in the alphabet as correct
                    SetLetterStatus(Convert.ToInt32(allGuesses[round - 1][i] - Convert.ToInt32('A')), CORRECT);

                }
            }

            //Evaluate for the letters that have appear in the word but do not have the correct position
            for (int i = 0; i < WORD_LENGTH; i++)
            {
                //Only check the letters that do not have the correct position
                if (statusOfGuesses[round - 1, i] != CORRECT)
                {
                    //Set the letter of the guess yellow or gray depending on whether the answer contains that letter 
                    if (tempAnswer.Contains(allGuesses[round - 1][i]))
                    {
                        //Set the status of that letter guess as being contained in the word and remove the letter from the temporary answer
                        statusOfGuesses[round - 1, i] = CONTAINS;
                        tempAnswer = tempAnswer.Remove(tempAnswer.IndexOf(allGuesses[round - 1][i]), 1);

                        //Change the status of that letter in the alphabet if the letter has not been correct
                        SetLetterStatus(Convert.ToInt32(allGuesses[round - 1][i] - Convert.ToInt32('A')), CONTAINS);
                    }
                    else
                    {
                        //Set the letter as not being in the word at all
                        statusOfGuesses[round - 1, i] = WRONG;

                        //Change the status of that letter if the letter has not been used
                        SetLetterStatus(Convert.ToInt32(allGuesses[round - 1][i] - Convert.ToInt32('A')), WRONG);
                    }
                }
            }
        }
    }
}
