<p align="center">
  <img src="logo.png" width="128" alt="logo" style="border-radius:16px;" />
</p>
<p align="center"><b>ACE</b></p>
<p align="center"><i>Adaptive Cardplay Engine</i></p>

## Overview

<b>ACE</b> is a modern, open-source C# library dedicated to the cardplay phase of the game of
<b>Bridge</b>, which is a classic example of an <i>imperfect information game</i> - games in which
players make decisions without full knowledge of the game state and where chance influences the
outcome - such games remain a persistent challenge in the field of AI.

One of the core issues in this domain, especially in card games, is <i>strategy fusion</i> -
a flaw where an engine evaluates many sampled deals separately and picks the move that scores best,
instead of choosing a plan that works across all indistinguishable scenarios. This often leads to
decisions that seem promising in simulation but fail in real-world play.

ACE mitigates this problem by incorporating concepts from the following paper:
https://arxiv.org/abs/2408.02380.
Rather than solving each sample with full information upfront, ACE delays the reasoning until
it is actually justified. This mechanism prevents overconfident decisions, reduces bias, and
leads to more realistic, human-like decisions.

## Features

ACE is still in an <b>experimental stage</b>, but already covers most practical use cases:

- <b>Support for any deal</b> – Analyzes games with any mix of <b>known</b> and <b>unknown</b> cards.
- <b>High-performance core</b> – Uses low-level bitwise operations for fast game processing.
- <b>Fast deal generator</b> – Quickly produces random deals for simulation and decision making.
- <b>Hand constraint system</b> – Filters deals by <b>HCP</b> and <b>suit lengths</b> <i>(more constraints coming soon)</i>.
- <b>Smart solving algorithm</b> – Based on research that avoids early commitment and strategy fusion.
- <b>Low memory consumption</b> – Uses a compact, efficient tree structure with minimal allocations.
- <b>Multithreading</b> – Runs simulations in parallel to accelerate large-scale analysis and sampling.
- <b>Backend-ready architecture</b> – Easily integrates with tools, bots, UIs, or research pipelines.
- <b>Runs on .NET Standard 2.0</b> – Cross-platform support for <b>Windows</b> and <b>Linux</b> (x64).

## History

Before starting ACE, I developed a separate project called <b>BGA (Bridge Gameplay Analysis)</b> –
a desktop application with a graphical interface focused on analyzing cardplay using a pure
PIMC-style algorithm with several domain-specific improvements. BGA supported only
<b>declarer play</b> and produced convincing results across many test cases.

The project was described in my BSc thesis titled
<i>"Desktop application for the analysis of gameplay in the card game of Bridge"</i>, defended in
March 2023 at the university <i>Politechnika Bydgoska im. Jana i Jędrzeja Śniadeckich</i> in Poland.

In fact, as noted in the thesis paper, BGA was able to solve
<b>more Bridge puzzles than a human expert</b> under the same conditions – demonstrating the practical
strength of the approach. However, BGA relied on heuristics, which in some edge cases led to
suboptimal decisions when evaluation failed to capture the true complexity of the position.

<b>ACE is a completely new project</b>, built from the ground up as a high-performance backend library
with a different architecture and philosophy. While it draws on insights gained from developing
BGA, it introduces new solving methods, supports both declarer and defender perspectives, and
focuses on reducing strategy fusion using more principled reasoning inspired by recent research.

## Installation

You can use ACE in two main ways:  
- As a library (DLL), integrated into your own .NET projects  
- As a standalone console application, for direct interactive use

### DLL Library

You can use ACE's library by adding the source code directly to your project or by compiling it as a local DLL.  
The project is built in a Visual Studio environment and can be run from source using the included launcher.

#### Requirements:
- Target framework: <b>.NET Standard 2.0</b> or higher
- <b>Windows</b> or <b>Linux</b> operating system (<b>x64</b> architecture)
- Native solver <code><b>libbcalcdds</b></code> must be available at runtime

Once installed or built, you can reference the compiled DLL in your .NET project like any standard library.  
This is currently the only way to use it - a NuGet package is not available yet, but planned for future release.  
Make sure to also include the native solver dependency, which is required at runtime to perform simulations.

You can find platform-specific versions in the <code>runtimes</code> folder:
- <code>runtimes/win-x64/native/libbcalcdds.dll</code> for <b>Windows x64</b>  
- <code>runtimes/linux-x64/native/libbcalcdds.so</code> for <b>Linux x64</b>

For detailed usage instructions, refer to the <code>lib</code> folder in the repository.

### Console Application

A standalone console application is included in the <code>app</code> folder, allowing you to use ACE without writing code.  
Designed for ease of use, the application provides direct access to ACE core through a command-line interface.  
To view available commands, simply compile and launch the app, then type <code>help</code> command at any prompt.

## License

This project is open-source and licensed under the GPL-3.0 license.  
See the <a href="LICENSE">LICENSE</a> file for details.
