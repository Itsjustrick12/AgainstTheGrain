## CMSC 437 Semester Project: Against The Grain

Against The Grain is a Top Down 2D game that blends elements of strategy and farming games to create unique logistical challenges for the player to overcome. The project was made with **Unity** and **C#**. 

The different units do different things:

Farmers:
- Green Farmer: Can only plant seeds, water crops, and harvest crops. Has a greater movement range
- Red Farmer: Can do everything the green farmer can in addition to being able to attack with 1 point of damage, -1 movement range

Animals:
- Chicken: Large Range of Motion, Deals 1 damage on attack. Best used in swarms. Costs one harvested crop to feed.
- Cow: Small Range of Motion, Deals 4 damage on attack. Big Bruiser. Costs two harvested crops to feed.

Animal Units must be fed first, then they become accessible for the player to move and utilize.

There is a downloadable zip file that contains an exe you can use to play the game, or you can play in the browser on [my itch.io page](https://itsjustrick12.itch.io/against-the-grain).

## Controls

You pick up and place down the characters with the mouse button, selecting valid tiles by hovering your mouse over them. Upon placing a unit down, you can see a few options. They are all quite self explanatory, though you can only attack enemy units if they are directly perpendicular to the selected unit. You can navigate the menus with the up and down arrow keys, and enter to select your choice. If you wish to end your turn early, you can press space. Additionally, if you click ESC, you can pull up the pause menu.

## Project Scope Disclosures

Due to my heavy course load this semester, I ended up doing most of the work on this project in a period of a month. I wasn't able to make the graphics too flashy or add many of the scope based features I initially pitched. The primary game functionality is present, though their are little hiccups in the quality of the implementation.

A few known shortcomings I was not able to address:
- Any kind of accessibility support outside of multiple options for inputs
- A smarter path finding algorithm for enemies
- Displaying the amount of harvested crops the player has reaped
- Displaying the cost associated with feeding an animal
- Displaying Attacking Stats for units
- Animations for character movement and attacking interactions
- More Scenarios / Levels
- An additional enemy unit variation
- Special moves for animals

Additionally, due to time constraints I had to quickly repurpose my UI code from previous Unity projects for scene navigation (title screen to game scene, quit buttons, etc) and I did not comment as much I did when I first started development. The logic gets pretty janky someplaces, mainly due to me just wanting a quick solution for the submission deadline.  