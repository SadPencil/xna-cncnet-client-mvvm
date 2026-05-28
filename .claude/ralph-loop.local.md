---
active: true
iteration: 8
session_id: 
max_iterations: 50
completion_promise: "MIGRATION_COMPLETE"
started_at: "2026-05-28T08:59:15Z"
---

You are an expert software engineer specializing in refactoring legacy codebases into modern architectural patterns. Your task is to analyze and refactor DXMainClient project (a C# XNA-based game client) into a strict MVVM (Model-View-ViewModel) architecture. The DXMainClient project is a large, monolithic codebase with tightly coupled UI, business logic, and external dependencies. Your refactoring must enforce (1) no business logic in View (2) no GUI-related code in ViewModel

The DXMainClient project will be erased in the end. Your refactor creates two new C# project, namely DXMainClientView, DXMainClientViewModel. The namespace should be DXMainClientView, DXMainClientViewModel respectively. DXMainClientView contains only View. DXMainClientViewModel contains ViewModel, Model, Service, etc. Each View, ViewModel, Service class must has a corresponding C# interface to abstract the methods. The interface is used for a view - view model communication, not a naive method abstraction of existing classes!

These two new project must be set to <Nullable>enable</Nullable>.

No business logic means, the View should only care about UI elements (e.g., text, is visible, etc.), not how the elements are controlled (e.g., on game started, disable some buttons). The view model controls the latter!!

My final objective is to replace the view with avaloniaui. You must think carefully what a View is.

You must use CommunityToolkit.Mvvm package to avoid re-introduce wrappers.

You must fully understand these requirements and store them in your memory.

Now, you task is, directly copy the whole DXMainClient into DXMainClientViewModel project, then remove view related code with observable properties/collections. You MUST make sure ALL non-View codes in DXMainClient into DXMainClientViewModel project. All means all -- except direct UI components managed by Views, all other codes must be completely migrated to DXMainClientViewModel. Not a single logic code should be left in View. DXMainClientViewModel must not rely on: ClientGUI, Microsoft XNA, MonoGame I plan to remove the DXMainClient in the end, so, be sure to move all non-GUI codes. Each view model must implement the interface respectively. You should not modify the interface unless absolutely necessary.

All means all -- except direct UI components managed by Views, all other codes must be completely migrated to DXMainClientViewModel. Not a single logic code should be left in View. You must remember what's a view and you must place this concept in your memory: for example, the ViewModel should NOT expose methods that the View calls. The ViewModel must be self-sufficient: it subscribes to services/events itself, and the View only observes properties and invokes commands. The view model must not expect the view calling anything other than double binded properties or a directly user-issued command. IViewModel must only expose what view can observe, and must not expect the view doing anything. View is just a view. View does not have any code logic and can't call anything. Write what's MVVM before doing it.

Please note that I require you to naively copy all codes from DXMainClient to DXMainClientViewModel project first, then subtract codes that supposed to be in View. Do follow my instruction.

DXMainClientViewModel must not rely on: ClientGUI, Microsoft XNA, MonoGame, DXMainClient, DXMainClientView. 

You should refer to file  to know what need to be done and what must not to be done. Recheck this file if you forget some details and are uncertain about the rules.

Git commit and push often. 

Piece by piece -- in each session, you can only choose ONE class (Only one file in DXMainClient!! you must read the whole class file and migrate it. You must not read a bunch of class files and migrate them at the same time.), make sure ALL codes except XNAUI elements are migrated, and commit it. You must never delete codes in DXMainClient project, and just ignore them when implementing view models, otherwise you will be unable to check later.

After you think you are done, re-check by comparing the DXMainClient with DXMainClientViewModel. There should be absolutely not a single logic code exist in DXMainClient but does not exist in DXMainClientViewModel. There should be absolutely no stub or logic-in-view assumption. The logic in DXMainClientViewModel must be identical with DXMainClient. 

When choosing the class you need to handle, if you are 100% certain ALL classes have being migrated to MVVM without any missing pieces (often they would have small logic forgot to be migrated. Don't be too optimistic.), print the exact string: <promise>MIGRATION_COMPLETE</promise>. Otherwise, you must not print this string, and select one class file to migrate instead.
