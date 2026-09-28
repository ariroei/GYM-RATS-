# GYM RATS agent workflow

GYM RATS is a stylized 3D PC party fighting game. Preserve existing work and use
the Unity version recorded in ProjectSettings/ProjectVersion.txt and the existing
Universal Render Pipeline configuration. Keep code, comments, project names,
documentation, and player-facing text in English.

After completing every future prompt that changes this project:

1. Inspect Git status and review the changes. Preserve unrelated and uncommitted
   user work; stage only the files relevant to the completed task.
2. Run the available relevant validation, including Unity compilation, tests,
   scene checks, or builds as appropriate. Report checks that could not run and
   any remaining failures; do not claim unperformed checks passed.
3. Create a descriptive Git commit for the completed work.
4. Push the commit to this repository's current working branch. Never force-push,
   overwrite unrelated changes, or create a separate repository. Do not create
   an empty commit when there are no changes.
5. Report the branch name, commit hash, push status, validation results, and any
   necessary Unity Editor steps. If authentication or permissions prevent the
   push, report the exact blocker and leave the commit ready to push.

Keep Unity .meta files with their assets. Do not commit generated Library, Temp,
Logs, UserSettings, or build output. Preserve asset GUIDs when moving files.
