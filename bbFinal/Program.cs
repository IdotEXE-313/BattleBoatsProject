using System;
using System.Text;
using System.Text.RegularExpressions;
using Figgle;
using Newtonsoft.Json;

namespace BattleBoats
{
    class Program
    {
        static void Main(string[] args)
        {
            Menu();
        }

        static void Menu()
        {
            /*
             * The boards that will be used throughout the game are initialized here because they are needed for the PlayGame function parameters
             * This is because, when we resume the game, we need someway of using the PlayGame function with the arrays deserialized in the ResumeGame function
             */
            string[,] playerBoard = new string[8, 8];
            string[,] playerTrackingBoard = new string[8, 8];
            string[,] computerBoard = new string[8, 8];
            string[,] computerTrackingBoard = new string[8, 8];
            
            Console.WriteLine("1.)Play Game\n2.)Read Instructions\n3.)Resume Game\n4.)Quit Game");
            bool userCheck = int.TryParse(Console.ReadLine(), out int userOption);

            if (userCheck && userOption <= 4 && userOption >= 1)
            {
                switch (userOption)
                {
                    case 1:
                        PlayGame(playerBoard, playerTrackingBoard, computerBoard, computerTrackingBoard);
                        break;
                    case 2:
                        Instructions();
                        break;
                    case 3:
                        ResumeGame();
                        break;
                    case 4:
                        Environment.Exit(0);
                        break;
                }
            }
            else
            {
                Console.WriteLine("Please input a valid option");
                Menu();
            }
        }
        
        static void Instructions()
        {
            Console.WriteLine(FiggleFonts.Standard.Render("Battle - Boats"));
            Console.WriteLine("1.)You will be displayed with a blank fleet\n" +
                              "2.)Then, you will be prompted to enter a coordinate to place your boats. You will prompted to do this 5 times with 5 unique locations.\n" +
                              "3.)Then, the computer will randomly generate five coordinates for it to place its own boats.\n" +
                              "4.)You will then be presented with a tracking board. This is where your hits and misses will be recorded\n" +
                              "5.)You will be prompted to input a coordinate to guess where a boat is stored on the computer's board. A hit or miss will be added to your tracking board accordingly\n" +
                              "6.)The computer will then guess a coordinate from your board which will be displayed. If the computer has already guessed this location, it will try and guess again.\n" +
                              "7.)A message is displayed, confirming whether the boat hit or missed one of your boats\n" +
                              "8.)This process continues until there is a winner. The winner will be output. Any data saved throughout the game will be wiped at the point someone wins");
            Thread.Sleep(10000);
            Menu();
        }
        
         
        static void PlayGame(string[,] playerBoard, string[,] playerTrackingBoard, string[,] computerBoard, string[,] computerTrackingBoard)
        {
            string coordinate = String.Empty;
            int checkForCompleteBoard = 0;
            PresentFleet(playerBoard);

            /*
             * This loop checks to see how many boats already exist within the PlayerBoard
             * This is because, if the player is resuming the game and hasn't finished placing their boats yet, we need to know how many more boats they need to place
             */
            for (int i = 0; i < playerBoard.GetLength(0); i++)
            {
                for (int j = 0; j < playerBoard.GetLength(1); j++)
                {
                    if (playerBoard[i, j] == "B")
                    {
                        checkForCompleteBoard++;
                    }
                }
            }

            /*
             * If there are not 5 boats on the board already, the loop sets i to the remainder of boats that need to be added
             * The user is then prompted for a coordinate. This is error checked, and if invalid, the user is prompted to enter again.
             */
            if (checkForCompleteBoard != 5)
            {
                for (int i = 0; i < (5 - checkForCompleteBoard); i++)
                {
                    coordinate = GetUserInput();
                    bool checkIfValid = CheckIfCoordinateIsValid(coordinate);

                    while (!checkIfValid)
                    {
                        coordinate = GetUserInput();
                        checkIfValid = CheckIfCoordinateIsValid(coordinate);
                    }

                    AddBoatToPlayerBoard(playerBoard, coordinate);
                    PresentFleet(playerBoard);
                    SaveProgress(playerBoard, playerTrackingBoard, computerBoard, computerTrackingBoard);
                }

                /*
                 * Loop adds 5 boats to the computer's board
                 * This loop will only ever need to be called once in an entire game. Once the user inputs their last boat, this process begins and the computerBoard is saved
                 * This means the user doesn't have time to quit the game. The computerBoard has either been made or hasn't been made, and will never be in the process of being made
                 */
                for (int i = 0; i < 5; i++)
                {
                    var coordinates = GetComputerCoordinates();
                    computerBoard = AddBoatToComputerBoard(computerBoard, coordinates);
                }
                SaveProgress(playerBoard, playerTrackingBoard, computerBoard, computerTrackingBoard);
            }

            
            bool gameProgress = true;
            while (gameProgress)
            {
                PresentFleet(playerTrackingBoard);
                UserGuess(playerTrackingBoard, computerBoard, playerBoard, computerTrackingBoard);
                if (CheckIfComputerLost(computerBoard))
                {
                    Console.WriteLine("Player has won!");
                    WipeProgressFile(); //Progress file is wiped here to ensure the game never resumes from the point where the game is already won
                    gameProgress = false;
                }
                else
                {
                    SaveProgress(playerBoard, playerTrackingBoard, computerBoard, computerTrackingBoard);
                
                    ComputerGuess(playerBoard, computerTrackingBoard);
                    if (CheckIfPlayerLost(playerBoard))
                    {
                        Console.WriteLine("Computer has won!");
                        WipeProgressFile(); //Again, progress file is wiped here to ensure the game never resumes from the point where the game is already won
                        gameProgress = false;
                    }
                    SaveProgress(playerBoard, playerTrackingBoard, computerBoard, computerTrackingBoard);
                }
            }

            Menu();
        }
        
        /*Responsible for returning the dictionary which will be used to convert the letter part of the coordinate into an index
         E.g., assume we have 'A1'. This dictionary will be responsible for converting 'A' to index 0 of a board*/
        private static Dictionary<string, int> GetLetterDictionary()
        {
            var letterConversions = new Dictionary<string, int>()
            {
                {"A", 0},
                {"B", 1},
                {"C", 2},
                {"D", 3},
                {"E", 4},
                {"F", 5},
                {"G", 6},
                {"H", 7}
            };

            return letterConversions;
        }

        /*
         * Responsible for converting the number part of the coordinate into an index
         * E.g., assume we have the coordinate 'A1', this dictionary will be responsible for converting '1' to index 0 of a board
         * When combined with the GetLetterDictionary function, this translates 'A1' to (0,0)
         */
        private static Dictionary<string, int> GetNumberDictionary()
        {
            var numberConversions = new Dictionary<string, int>()
            {
                {"1", 0},
                {"2", 1},
                {"3", 2},
                {"4", 3},
                {"5", 4},
                {"6", 5},
                {"7", 6},
                {"8", 7}
            };

            return numberConversions;
        }
        
        //Outputs the 2d array in a table-like format, using StringBuilder in order to output the row (1,2,3...8) alongside the array itself
        private static void PresentFleet(string[,] board)
        {
            string[] tableColumnOutput = {"A", "B", "C", "D", "E", "F", "G", "H"};
            string[] tableRowOutput = {"1", "2", "3", "4", "5", "6", "7", "8"};
            
            Console.WriteLine("\t" + string.Join("\t", tableColumnOutput)); 
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < tableRowOutput.Length; i++)
            {
                sb.Append(tableRowOutput[i] + "\t");  
                for (int j = 0; j < board.Length / tableRowOutput.Length; j++) 
                    sb.Append(board[i, j] + "\t");

                Console.WriteLine(sb.ToString()); 
                sb.Clear();
            }
        }

        private static string GetUserInput()
        {
            Console.Write("Enter a coordinate for your boat to go in: ");
            string coordinate = Console.ReadLine().ToUpper();
            return coordinate;
        }

        private static bool CheckIfCoordinateIsValid(string coordinate)
        {
            var regexCharacter = new Regex(@"[A-Z]");
            var regexNumber = new Regex(@"[0-9]");
            var letterConversions = GetLetterDictionary();
            var numberConversions = GetNumberDictionary();
            
            /*
             * Returns false is coordinate length is not equal to 2...
             * Or if the first character of the coordinate is not equal to an upper case letter....
             * Or if the second character of the coordinate is not equal to a number from 0 - 9....
             * Or if the first character of the coordinate does not match a key in the LetterConversions dictionary....
             * Or if the second character of the coordinate does not match a key in the NumberConversions dictionary
             */
            if (coordinate.Length != 2 || !regexCharacter.IsMatch(coordinate[0].ToString()) || !regexNumber.IsMatch(coordinate[1].ToString()) || !letterConversions.ContainsKey(coordinate[0].ToString()) || !numberConversions.ContainsKey(coordinate[1].ToString()))
            {
                Console.WriteLine("Invalid coordinate. Please re-enter");
                return false;
            }
            return true;
        }

        /*
         * A trend that follows from here:
         * You may notice throughout the program that, when placing a coordinate on the board, the coordinate is flipped
         * E.g., if we have G5, this index is flipped to match the index of 5G.
         * This is because of the way the board is presented. Had the index not been swapped, G5 would go to (6,4) on the board which is actually coordinate E7
         */
        private static string[,] AddBoatToPlayerBoard(string[,] playerBoard, string coordinate)
        {
            var letterConversions = GetLetterDictionary();
            var numberConversions = GetNumberDictionary();
            var regexCharacter = new Regex(@"[A-Z]");
            var regexNumber = new Regex(@"[0-9]");
            int indexCharacter = 0, indexNumber = 0;

            /*
             * Searches the coordinate input, checking to see whether the current character is a letter or number
             * Then, the corresponding index for the letter / number is fetched from the appropriate dictionary
             */ 
            foreach (var character in coordinate)
            {
                if (regexCharacter.IsMatch(character.ToString()))
                {
                    indexCharacter = letterConversions[character.ToString()];
                }
                else if (regexNumber.IsMatch(character.ToString()))
                {
                    indexNumber = numberConversions[character.ToString()];
                }
            }

            if (playerBoard[indexNumber, indexCharacter] == "B")
            {
                Console.WriteLine("You have already placed a boat here! Try again");
                string coordinateReattempt = GetUserInput();
                AddBoatToPlayerBoard(playerBoard, coordinateReattempt);
            }
            
            playerBoard[indexNumber, indexCharacter] = "B";
            return playerBoard;
        }

        private static Tuple<int, int> GetComputerCoordinates()
        {
            var random = new Random();
            var indexCharacter = random.Next(0, 8);
            var indexNumber = random.Next(0, 8);

            return Tuple.Create(indexCharacter, indexNumber);
        }

        private static string[,] AddBoatToComputerBoard(string[,] computerBoard, Tuple<int, int> coordinates)
        {
            if (computerBoard[coordinates.Item2, coordinates.Item1] == "B")
            {
                var coordinatesReattempt = GetComputerCoordinates();
                AddBoatToComputerBoard(computerBoard, coordinatesReattempt);
            }

            computerBoard[coordinates.Item2, coordinates.Item1] = "B";
            return computerBoard;
        }

        private static string[,] UserGuess(string[,] playerTrackingBoard, string[,] computerBoard, string[,] playerBoard, string[,] computerTrackingBoard)
        {
            Console.Write("Guess the computer's boat location: ");
            var coordinate = Console.ReadLine().ToUpper();
            
            bool checkIfValid = CheckIfCoordinateIsValid(coordinate);
            var letterConversions = GetLetterDictionary();
            var numberConversions = GetNumberDictionary();
            var regexLetter = new Regex(@"[A-Z]");
            var regexNumber = new Regex(@"[0-9]");
            int indexLetter = 0, indexNumber = 0;

            while (!checkIfValid)
            {
                Console.WriteLine("Coordinate input invalid. Please ensure you input a coordinate corresponding to a coordinate on the board (e.g. G5): ");
                coordinate = GetUserInput();
                checkIfValid = CheckIfCoordinateIsValid(coordinate);
            }

            foreach (var character in coordinate)
            {
                if (regexLetter.IsMatch(character.ToString()))
                {
                    indexLetter = letterConversions[character.ToString()];
                }
                else if (regexNumber.IsMatch(character.ToString()))
                {
                    indexNumber = numberConversions[character.ToString()];
                }
            }

            if (playerTrackingBoard[indexNumber, indexLetter] != null)
            {
                Console.WriteLine("You have already guessed here! Try again");
                UserGuess(playerTrackingBoard, computerBoard, playerBoard, computerTrackingBoard);
            }

            if (computerBoard[indexNumber, indexLetter] == "B")
            {
                Console.WriteLine("You have hit the computer's boat!");
                playerTrackingBoard[indexNumber, indexLetter] = "H";
                computerBoard[indexNumber, indexLetter] = "";
            }
            else
            {
                Console.WriteLine("You missed");
                playerTrackingBoard[indexNumber, indexLetter] = "M";
            }

            return playerTrackingBoard;
        }

        private static string[,] ComputerGuess(string[,] playerBoard, string[,] computerTrackingBoard)
        {
            var coordinates = GetComputerCoordinates();
            var letterConversions = GetLetterDictionary();
            var numberConversions = GetNumberDictionary();
            
            //Fetches the Key by the value of the dictionary. This is so we can output the coordinates in which computer is guessing rather than the index in which they are guessing
            var letter = letterConversions.FirstOrDefault(x => x.Value == coordinates.Item1).Key;
            var number = numberConversions.FirstOrDefault(x => x.Value == coordinates.Item2).Key;

            Console.WriteLine($"Computer has guessed coordinate {letter + number}");

            if (computerTrackingBoard[coordinates.Item2, coordinates.Item1] != null)
            {
                Console.WriteLine("Computer has already guessed here!");
                Thread.Sleep(2000);
                ComputerGuess(playerBoard, computerTrackingBoard);
            }

            else if (playerBoard[coordinates.Item2, coordinates.Item1] == "B")
            {
                Console.WriteLine("Computer has hit!");
                playerBoard[coordinates.Item2, coordinates.Item1] = "H";
                computerTrackingBoard[coordinates.Item2, coordinates.Item1] = "H";
            }
            else
            {
                Console.WriteLine("Computer has missed");
                computerTrackingBoard[coordinates.Item2, coordinates.Item1] = "M";
            }

            return computerTrackingBoard;
        }

        /*
         * Applies to both CheckIfPlayerLost and CheckIfComputerLost
         * Every index of the board is checked for an instance of a remaining boat
         * Clearly, if there is a boat left, then this player hasn't had all their boats sunk and thus haven't lost the game
         */
        private static bool CheckIfPlayerLost(string[,] playerBoard)
        {
            for (int i = 0; i < playerBoard.GetLength(0); i++)
            {
                for (int j = 0; j < playerBoard.GetLength(1); j++)
                {
                    if (playerBoard[i, j] == "B")
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private static bool CheckIfComputerLost(string[,] computerBoard)
        {
            for (int i = 0; i < computerBoard.GetLength(0); i++)
            {
                for (int j = 0; j < computerBoard.GetLength(1); j++)
                {
                    if (computerBoard[i, j] == "B")
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /*
         * This method takes advantage of the Newtonsoft json serializer
         * The reason I chose this over writing as plain text is because it is far easier to convert a json object back into a 2d array when the game is resumed
         */
        private static void SaveProgress(string[,] playerBoard, string[,] playerTrackingBoard, string[,] computerBoard, string[,] computerTrackingBoard)
        {
            using (StreamWriter sw = new StreamWriter(@"progressFile", false))
            {
                for (int i = 0; i < 4; i++)
                {
                    switch (i)
                    {
                        case 0:
                            sw.Write(JsonConvert.SerializeObject(playerBoard)); //Converts the appropriate board into a json object and writes it to a file
                            sw.Write("\n");
                            break;
                        case 1:
                            sw.Write(JsonConvert.SerializeObject(playerTrackingBoard));
                            sw.Write("\n");
                            break;
                        case 2:
                            sw.Write(JsonConvert.SerializeObject(computerBoard));
                            sw.Write("\n");
                            break;
                        case 3:
                            sw.Write(JsonConvert.SerializeObject(computerTrackingBoard));
                            break;
                    }
                }
            }

        }

        private static void ResumeGame()
        {
            try
            {
                if (new FileInfo(@"progressFile").Length == 0)
                {
                    Console.WriteLine("You have no data saved!");
                    Thread.Sleep(5000);
                    Menu();
                }
            }
            catch (FileNotFoundException) //This is an exception which only occurs when the file has never been created and option 3 is selected. This is because the file doesn't exist yet
            {
                Console.WriteLine("Cannot resume a game that's never been played! Please play and place at least one boat to resume a game");
                Menu();
            }
            
            
            string[,] playerBoard = new string[8, 8];
            string[,] playerTrackingBoard = new string[8, 8];
            string[,] computerBoard = new string[8, 8];
            string[,] computerTrackingBoard = new string[8, 8];
            
            using (StreamReader sr = new StreamReader(@"progressFile"))
            {
                string line;
                int currentArray = 0;
                while((line = sr.ReadLine())!= null)
                {
                    switch (currentArray)
                    {
                        case 0:
                            playerBoard = JsonConvert.DeserializeObject<string[,]>(line); //Deserializes the json object straight back into a 2d array
                            break;
                        case 1:
                            playerTrackingBoard = JsonConvert.DeserializeObject<string[,]>(line);
                            break;
                        case 2:
                            computerBoard = JsonConvert.DeserializeObject<string[,]>(line);
                            break;
                        case 3:
                            computerTrackingBoard = JsonConvert.DeserializeObject<string[,]>(line);
                            break;
                    }

                    currentArray++;
                }
            }
            
            PlayGame(playerBoard, playerTrackingBoard, computerBoard, computerTrackingBoard);
        }

        
        private static void WipeProgressFile()
        {
            File.WriteAllText(@"progressFile", String.Empty);
        }
        
    }
}