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
    AxClientMvvmContract["AxClientMvvmContract"]
    AxClientView["AxClientView"]
    AxClientViewModel["AxClientViewModel"]
    AxClientExe["AxClientExe"]

    %% Dependencies
    AxClientExe --> AxClientMvvmContract
    AxClientExe --> AxClientView
    AxClientExe --> AxClientViewModel

    AxClientMvvmContract --> RampastringTools

    AxClientView --> AxClientMvvmContract
    AxClientView --> ClientCore

    AxClientViewModel --> ClientCore
    AxClientViewModel --> ClientUpdater
    AxClientViewModel --> AxClientMvvmContract

    ClientCore --> RampastringTools
    ClientUpdater --> ClientCore

    %% Styling
    classDef default fill:#f0f0f0,stroke:#333,stroke-width:2px,rx:8,ry:8;
```