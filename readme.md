# RARP Editor (RetroAchievements Rich Presence Editor)

**RARP Editor is a user-friendly desktop application designed to create and edit RetroAchievements Rich Presence (`*-Rich.txt`) scripts. It transforms the process of writing complex rich presence logic from manual text editing into a structured, intuitive, and error-resistant visual experience.**

This tool is built for both beginners and experienced achievement creators, providing real-time validation, a live preview, and specialized editors for every aspect of a rich presence script.

![RARP Editor Screenshot](https://i.imgur.com/bMnmluJ.png)

## Key Features

-   **Structured Project View:** See your entire script—Lookups, Formatters, and Display Logic—at a glance in a clean, organized tree view.
-   **Visual Logic Editor:** A grid-based editor that deciphers the complex trigger syntax (`R:0xH1234=1_N:p:0xH1234>0`) into a human-readable table. No more manual syntax wrestling!
-   **Live Preview:** Get immediate feedback on what your display string will look like. The preview panel simulates game values and updates on a timer to show how formatters and lookups will behave.
-   **Real-time Validation & Help:** The editor instantly validates your logic as you work. A dedicated panel provides contextual help and clear error/warning messages, guiding you to a perfect script.
-   **Full Undo/Redo Support:** Every change is recorded, allowing you to step backward and forward through your entire editing session.
-   **Dedicated Lookup & Formatter Management:**
    -   Easily manage key-value pairs for your lookups.
    -   Organize lookup entries into categories for better clarity.
    -   Quickly define formatters for displaying scores, times, frames, and more.
-   **Drag & Drop Reordering:** Intuitively change the evaluation order of your display strings by simply dragging them in the project explorer.

## Getting Started

1.  Navigate to the [**Releases**](https://github.com/CySlaytor/RARPEditor/releases) page.
2.  Download the latest `.exe` file.
3.  Run `RARPEditor.exe`.

## How to Use

1.  Go to `File > Open` to load an existing `*-Rich.txt` file, or `File > New` to start from scratch.
2.  The **Project Explorer** on the left will populate with all the lookups, formatters, and display strings from the file.
3.  **Double-click** any item in the explorer to open its dedicated editor in the central panel.
4.  Make your changes using the visual editors.
    -   The **Live Preview** panel will update in real-time if you are editing a display string.
    -   The **Help and Validation** panel will provide guidance and point out any errors.
5.  Once you are finished, go to `File > Save` or `File > Save As` to save your work.

Note: .NET 8 is required, [![.NET](https://img.shields.io/badge/.NET-8.0-blueviolet)](https://dotnet.microsoft.com/)

## Building from Source

If you want to contribute or build the project yourself, follow these steps:

#### Prerequisites

-   [.NET 8.0 SDK (or newer)](https://dotnet.microsoft.com/download/dotnet/8.0)
-   [Visual Studio 2022](https://visualstudio.microsoft.com/) with the ".NET desktop development" workload installed.

#### Steps

1.  Clone the repository:
    ```bash
    git clone https://github.com/CySlaytor/RARPEditor.git
    ```
2.  Open the `RARPEditor.sln` solution file in Visual Studio.
3.  Build the solution (`Ctrl+Shift+B` or `Build > Build Solution`).
4.  Run the project (`F5` or the "Start" button).

## Contributing

Contributions are welcome! Any contribtions include reporting a bug, suggesting a new feature, or submitting a pull request, your help is appreciated.

## License

This project is licensed under the MIT License. See the `LICENSE` file for more details. You are free to use, modify, and distribute this software.

## Acknowledgments

-   A huge thank you to the **RetroAchievements community** for creating and maintaining the platform that makes this all possible.