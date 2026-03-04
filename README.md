# KelimeModum-WordMode
This project is a comprehensive word-guessing puzzle game developed entirely by me using Unity and C#, covering both technical architecture and UI/UX design.

Core Gameplay & Modes
The main menu provides a customizable experience with 4, 5, and 6-letter word options. Players can choose from three distinct game modes based on their preference:

Word of the Day: A curated daily challenge that offers the highest point rewards. This mode includes a Streak System that tracks and rewards consecutive daily wins to encourage player retention.

Endless Mode: A continuous gameplay experience unlocked after surpassing a specific score threshold.

Timed Mode: A high-stakes mode where players must find the word within a default or adjustable time limit. Points are scaled based on the speed of the correct guess (faster guesses yield higher scores).

Life and Economy System
To balance the challenge, the game features a Life (Health) Mechanism:

Players start with a set number of lives.

Failing to guess the word within the allowed attempts results in the loss of a life.

When all lives are depleted, access to certain game modes is restricted until lives are replenished, adding a strategic layer to each attempt.

Game Mechanics & Dictionary
The game logic is supported by meticulously prepared JSON-formatted dictionaries. Players have 6 attempts to identify the target word using a color-coded feedback system:

Green: The letter is correct and in the right position.

Yellow: The letter exists in the word but is in the wrong position.

Gray: The letter does not exist in the word.

Visual Feedback: If a word not found in the dictionary is entered, the row shakes and flashes red to indicate an invalid entry.

Hard Mode (Multiplier)
Players can activate Hard Mode to double their points across all game modes. This mode introduces a strict constraint: letters marked as gray in previous attempts cannot be reused in subsequent guesses, forcing the player to utilize a broader range of vocabulary.

Customization & Settings
The settings menu ensures a personalized user experience, featuring:

User Profile management and progress tracking.

Sound Effects (SFX) toggle for audio customization.

How to Play instructions for seamless onboarding.

Developed and Designed by: Emine Kocabaş
Tech Stack: Unity & C#
