# CnCNet Client

**MVVM migration is in progress. This branch is in a very early stage, and is quite away from completed. Do not use it now. Also, the commits will be squashed in the end.**

```diff
-Rampastring.XNAUI
-ClientGUI
-DXMainClient
+AvClientMvvmContract
+AvClientViewModel
+AvClientView
+AvClientExe
```

```mermaid
flowchart TD
    %% Nodes
    RampastringTools["Rampastring.Tools"]
    ClientCore["ClientCore"]
    ClientUpdater["ClientUpdater"]
    AvClientMvvmContract["AvClientMvvmContract"]
    AvClientView["AvClientView"]
    AvClientViewModel["AvClientViewModel"]
    AvClientExe["AvClientExe"]

    %% Dependencies
    AvClientExe --> AvClientMvvmContract
    AvClientExe --> AvClientView
    AvClientExe --> AvClientViewModel

    AvClientMvvmContract --> RampastringTools

    AvClientView --> AvClientMvvmContract
    AvClientView --> ClientCore

    AvClientViewModel --> ClientCore
    AvClientViewModel --> ClientUpdater
    AvClientViewModel --> AvClientMvvmContract

    ClientCore --> RampastringTools
    ClientUpdater --> ClientCore

    %% Styling
    classDef default fill:#f0f0f0,stroke:#333,stroke-width:2px,rx:8,ry:8;
```