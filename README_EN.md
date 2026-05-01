# PRAGMATA Co-op Assistant

## What Is This

PRAGMATA is a Capcom game that combines third-person shooting with grid-based puzzle mini-games. In the puzzle portions, you need to use four directional inputs to navigate a grid. This tool splits those duties between two players: one person handles shooting with the first controller, while the other manages the puzzles with the second controller. The game sees only one virtual controller, yet the inputs from both physical controllers are merged behind the scenes before being sent to it.

## Prerequisites

You need to install three things on your computer.

First, the ViGEmBus driver. Our program needs to create a virtual Xbox 360 controller for the game to recognize, and this driver handles that. You have two ways to install it: download the ViGEmBus installer from its release page and run it, or use the package manager winget by running `winget install Nefarius.ViGEmBus` in a command prompt. Make sure it is installed before launching our application.

Second, the HidHide driver. This driver hides your physical controllers so the game cannot see them directly. Again, two options: download the installer from its release page, or run `winget install Nefarius.HidHide` in a command prompt. You must restart your computer after installing this one.

Third, the .NET 8.0 Desktop Runtime. Our program is written in C# and requires this runtime to execute. Head to Microsoft's official website and download the .NET 8.0 Desktop Runtime. Be sure to pick the desktop version, not the SDK.

## How to Use

Open the program with administrator privileges. Right-click PragmataCoop.exe and select "Run as administrator". HidHide configuration requires elevated rights, so this step is essential.

Plug both of your controllers into the computer. The program starts scanning for controllers automatically as soon as it opens. You will see prompts in the interface: the Controller 1 area says "Move Hugh's left stick" and the Controller 2 area says "Move Diana's right stick". Follow these prompts: wiggle the left stick on the first controller to tag it as Controller 1, then wiggle the right stick on the other controller to tag it as Controller 2. Once both are detected, a green checkmark and slot number appear next to each.

Choose your preferred mapping mode from the dropdown menu. There are three options. In Right Stick mode, the right stick on Controller 2 maps its four directions to the virtual Y, A, X, and B buttons. This is the recommended setting. In Buttons mode, Controller 2's physical A, B, X, and Y buttons map directly to the virtual equivalents. In Mixed mode, both the stick and the buttons work simultaneously.

Click the Start button. The program creates a virtual Xbox 360 controller named PragmataVirtualController, then uses HidHide to conceal both physical controllers from the operating system. It is a good idea to unplug and replug both controllers right after clicking Start, so that HidHide's new filtering rules take effect immediately.

Now launch the game. The game sees only the single virtual controller. Controller 1 handles nearly all the shooting actions, passing them through the virtual controller to the game. Controller 2's left trigger acts as a mode switch: when you hold LT on Controller 2, the right stick or face buttons on Controller 2 take over the puzzle controls; when you release LT, Controller 1 regains full command.

While puzzle mode is active (Controller 2 holding LT), Controller 1's left stick, right stick, triggers, shoulder buttons, D-pad, and menu buttons continue to work normally. Only the A, B, X, and Y buttons on Controller 1 are suppressed. The two operators do not interfere with each other.

If the puzzle controls seem unresponsive while holding LT on Controller 2, try pressing LT a bit harder. The activation threshold sits at roughly a quarter of the full trigger travel.

To stop, click the Stop button or simply close the window. The program destroys the virtual controller and disables HidHide filtering, returning everything to normal.

## How It Works

At its core, this program is an input forwarder. It listens to both physical controllers simultaneously, combines their inputs according to a set of rules, computes a single merged controller state, and writes that merged state into a virtual controller. When the game starts up, it only discovers the virtual controller, so it believes a single person is playing.

Physical controllers are read through XInput, the standard gamepad API built into Windows. The program calls into the system's XInput DLL directly from C#, reads the raw controller data into memory, and parses it by hand. We learned the hard way that passing C# structs across the native boundary can cause memory alignment issues, so we switched to reading raw bytes and parsing them with BitConverter, which completely sidesteps the P/Invoke struct marshaling layer.

The virtual controller is created using ViGEmBus, a kernel-level virtual device driver capable of producing a virtual Xbox 360 controller that is indistinguishable from a real one. Our program uses ViGEm's C# client library to write stick positions, trigger values, and button states into the virtual controller.

The input merging logic is straightforward. When Controller 2 is not holding LT, all inputs flow from Controller 1, and Controller 2 is ignored entirely except for its LT, which is monitored to detect activation. When Controller 2 presses LT to enter puzzle mode, Controller 1's four face buttons are suppressed, and Controller 2 takes them over according to the selected mapping mode. Controller 1's sticks, triggers, shoulders, and other controls continue to pass through normally. Controller 2's LT is also merged into the virtual controller's LT during puzzle mode, so the game receives the trigger input it expects for certain actions.

The hardest problem was preventing the game from seeing the physical controllers at all. When a game enumerates connected devices, it finds every controller plugged into the system, both physical and virtual. If the game can read the physical controllers directly, the face buttons can still reach the game, defeating our suppression logic. To solve this, we introduced HidHide, a kernel-level HID filter driver.

HidHide intercepts application requests for HID devices at the Windows kernel level. We use its API to scan every HID and USB device interface present on the system, adding everything except our virtual controller and common keyboard and mouse devices to a block list. We also add our own program to an allow list, so it alone retains the ability to read the raw physical controller data. From the game's perspective, the physical controllers simply cease to exist.

There is a subtle detail here: HidHide must block instances at both the HID and USB levels. Blocking only HID interfaces is insufficient, because some applications that use DirectInput or access the USB driver directly can still discover the controller through the USB device node. Our program therefore uses the Nefarius.Utilities.DeviceManagement library to enumerate interfaces at both levels and adds them all to the block list.

There is also a trap we fell into during development. The ViGEmBus virtual controller started out mimicking a stock Xbox 360 controller with a standard Microsoft vendor and product ID, and its USB instance ID happened to contain the string DC5B7401. Some third-party Xbox-compatible controllers, such as the Betop Zeus T6, also report this exact same USB serial number. When we naively excluded any device containing DC5B7401 from the block list to avoid hiding our own virtual controller, we accidentally exempted the physical T6 controller as well. Removing that overly broad exclusion fixed the problem. We also gave the virtual controller a custom vendor and product ID pair, which not only avoids collisions but also makes it immediately recognizable in the HidHide configuration tool.

The user interface is built with WPF, driven by a ViewModel layer with data binding. The controller detection state machine has two phases: first it discovers Controller 1 by watching for left-stick movement, then it discovers Controller 2 by watching for right-stick movement on a different XInput slot. Once both are found, the user clicks Start, the program creates the virtual controller, configures HidHide, and enters the mapping loop. The mapping loop polls the XInput state of both controllers every sixteen milliseconds, computes the combined virtual controller state, and writes it into the ViGEm driver.

Shutdown runs the sequence in reverse: the mapping loop stops first, the virtual controller is destroyed, and HidHide filtering is deactivated. This leaves the system clean for the next launch.

We also explored whether REFramework, a popular modding framework for the RE Engine, could be used to block controllers from inside the game process. The idea was to hook into the game's via.hid.GamePad class and force it to read only the virtual controller. Testing revealed that Pragmata does not call the get_Device or get_MergedDevice methods on this class for actual input processing. We then tried REFramework's C# plugin system and per-frame callbacks, but the MergedGamePadDevice type had no fields that could be modified. The game reads controller input at the C++ level, directly through the XInput API, without going through any method that can be intercepted by C# or Lua. This dead end confirmed that the problem could only be solved at the kernel level.

The program supports both Chinese and English, automatically detecting the system language at startup. A manual language selector at the bottom of the window allows on-the-fly switching without restarting.
