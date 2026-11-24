<div align="center">

<h1>Journey</h1>

<p><b>A card-swiping football career game in the style of Reigns.</b><br/>
Live a footballer's whole career, one decision at a time.</p>

<p>
<img src="https://img.shields.io/badge/status-in%20development-E8833A?style=flat-square" alt="Status: in development"/>
<img src="https://img.shields.io/badge/languages-EN%20%7C%20TR-2F6F8F?style=flat-square" alt="Languages: English and Turkish"/>
<img src="https://img.shields.io/badge/Unity-2022.3%20LTS-24292f?style=flat-square&logo=unity&logoColor=white" alt="Unity 2022.3 LTS"/>
<img src="https://img.shields.io/badge/C%23-24292f?style=flat-square&logo=dotnet&logoColor=white" alt="C#"/>
</p>

<p><a href="https://bahadirkocatas.itch.io/journey-football-career"><img src="https://img.shields.io/badge/Play%20on%20itch.io-FA5C5C?style=for-the-badge&logo=itchdotio&logoColor=white" alt="Play on itch.io"/></a></p>

<img src="docs/media/gameplay.gif" alt="Swiping through career cards" width="240"/>
<img src="docs/media/crisis.png" alt="A crisis card" width="240"/>
<img src="docs/media/career.png" alt="Player profile and career history" width="240"/>

</div>

---

## How it plays

Every card is a moment from a player's life: the coach, the agent, the press, the dressing room. Swipe left or right to choose. Each choice moves five resources, and pushing any of them to zero or to the maximum starts a crisis. Seasons run week by week, through transfers between clubs and Champions League nights, until the career comes to an end.

| ⚡ Form | 💰 Money | ⭐ Fame | ❤️ Relations | 🎯 Focus |
|:---:|:---:|:---:|:---:|:---:|

## At a glance

| 1,513 | 40 | 16 | 2 |
|:---:|:---:|:---:|:---:|
| cards, 481 of them for the Champions League | clubs in 6 countries | crisis events | languages, English and Turkish |

## Architecture

The code is organised in layers with one-way dependencies: a layer may use the layers below it, never the ones above.

```mermaid
flowchart LR
    UI[Game.UI] --> Gameplay[Game.Gameplay]
    Gameplay --> Services[Game.Services]
    Services --> Core[Game.Core]
    Core --> Data[Game.Data]
```

| Decision | Why |
|---|---|
| `GameStateMachine` with a `GameState` enum | One source of truth for the game flow instead of scattered boolean flags |
| Central `GameEvents` bus | All in-game communication goes through one place |
| `GameConstants` | Keys and constants live in one place instead of magic strings |
| Content in JSON, read with Newtonsoft.Json | Cards, clubs, crises and endings are data. Card categories are strings, so new ones can be added without code changes |
| `Singleton<T>` and `PersistentSingleton<T>` | Every manager follows the same lifecycle |

> **Status:** The project is in the middle of a planned refactor toward this architecture. The data and service layers are done and the gameplay layer is in progress.

## Running the project

Open the folder with **Unity 2022.3.62f2** and open `Assets/Scenes/SampleScene.unity`.
