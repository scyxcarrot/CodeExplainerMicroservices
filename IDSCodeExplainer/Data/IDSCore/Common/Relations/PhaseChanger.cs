using IDS.Core.Enumerators;
using IDS.Core.ImplantDirector;
using IDS.Core.PluginHelper;
using Rhino.UI;

namespace IDS.Core.Relations
{
    public class PhaseChanger
    {
        private static void ClearUndoRedoHistory(IImplantDirector director)
        {
            // Clear undo history
            director.Document.ClearUndoRecords(true);
            director.Document.ClearRedoRecords();
            director.IdsDocument?.ClearUndoRedo();
        }
        
        /// <summary>
        /// Asks the confirmation to start from higher phase.
        /// </summary>
        /// <param name="targetPhase">The target phase.</param>
        /// <returns></returns>
        public static bool AskConfirmationToStartFromHigherPhase(DesignPhaseProperty targetPhase)
        {
            // No need to ask if user is in scriptmode
            if (IDSPluginHelper.ScriptMode)
                return true;

            // Get target phase name
            string targetPhaseName = targetPhase.Name;

            // Show a dialog
            var result = Rhino.UI.Dialogs.ShowMessage(
                        string.Format("Making changes in the {0} phase will invalidate parts of the design that were created in later phases. Are you sure you want to switch to the {0} phase?", targetPhaseName),
                        string.Format("Switching to {0} phase", targetPhaseName),
                        ShowMessageButton.YesNo,
                        ShowMessageIcon.Exclamation);

            // Only enter phase if user pressed yes
            if (result == ShowMessageResult.Yes)
            {
                return true;
            }

            // success
            return false;
        }

        private static bool AskTransitionConfirmation(DesignPhaseProperty currentPhase, DesignPhaseProperty targetPhase, bool confirmFromHigher)
        {
            bool confirmed = true;

            // If you are already in the target phase, abort and show a message
            if (currentPhase.Value == targetPhase.Value)
            {
                IDSPluginHelper.WriteLine(LogCategory.Default, string.Format("You are already in the {0} Phase.", targetPhase.Name));
                confirmed = false;
            }

            // If coming from a higher phase, before doing any events, ask user if he's sure
            else if (currentPhase.Value > targetPhase.Value && confirmFromHigher)
            {
                confirmed = AskConfirmationToStartFromHigherPhase(targetPhase);
            }

            return confirmed;
        }

        /// <summary>
        /// General method for changing phases
        /// 1. Phase change to targetPhase is asked
        /// 2. If you are already in targetPhase, abort
        /// 3. If targetPhase > currentPhase, ask if a user is sure, otherwise abort
        /// 4. If targetPhase is DevelopmentPhase, ask if a user is sure, otherwise abort
        /// 5. Call the stop event of the currentPhase, if it fails, abort
        /// 6. Change the current phase to the targetPhase
        /// 7. If targetPhase was lower than currentPhase, invoke FromDown action
        /// 8. If targetPhase was higher than currentPhase, invoke FromUp action
        /// 9. Call the 'Both' action
        /// 10. All done
        /// </summary>
        /// <param name="director">The director.</param>
        /// <param name="targetPhase">The target phase.</param>
        /// <param name="askConfirmation">if set to <c>true</c> [ask confirmation].</param>
        /// <returns></returns>
        public static bool ChangePhase(IImplantDirector director, DesignPhaseProperty targetPhase, bool askConfirmation = true)
        {
            // init
            DesignPhaseProperty currentPhase = director.CurrentDesignPhaseProperty;

            bool continueTransition = AskTransitionConfirmation(currentPhase, targetPhase, askConfirmation);
            if(continueTransition)
            {
                // Perform stop actions for current phase
                bool stoppedCurrentPhase = StopCurrentPhase(director, targetPhase);
                if (!stoppedCurrentPhase)
                {
                    IDSPluginHelper.WriteLine(LogCategory.Default, string.Format("Unable to break out of the {0} Phase.", director.CurrentDesignPhaseProperty.Name));
                    return false;
                }

                // Enter the new design phase
                director.EnterDesignPhase(targetPhase);
                IDSPluginHelper.WriteLine(LogCategory.Default, string.Format("Entering the {0} Phase...", targetPhase.Name));

                // Perform start actions for target phase
                bool startedTargetPhase = StartTargetPhase(director, currentPhase, targetPhase);
                if (!startedTargetPhase)
                {
                    IDSPluginHelper.WriteLine(LogCategory.Default, string.Format("Unable to start the {0} Phase.", targetPhase.Name));
                    return false;
                }

                // Clear undo/redo history
                ClearUndoRedoHistory(director);

                // Success
                IDSPluginHelper.WriteLine(LogCategory.Default, string.Format("Successfully entered the {0} Phase.", targetPhase.Name));
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Starts the target phase.
        /// </summary>
        /// <param name="director">The director.</param>
        /// <param name="currentPhase">The current phase.</param>
        /// <param name="targetPhase">The target phase.</param>
        /// <returns></returns>
        private static bool StartTargetPhase(IImplantDirector director, DesignPhaseProperty currentPhase, DesignPhaseProperty targetPhase)
        {
            // Call the phase enter event You are coming from a lower phase
            if (currentPhase.Value < targetPhase.Value)
            {
                // if there is an action to be taken, do it
                if (targetPhase.StartActionFromDown != null)
                {
                    bool performedStartFromDownActions = targetPhase.StartActionFromDown(director);
                    if (!performedStartFromDownActions)
                        return false;
                }
            }
            // You are coming from a higher phase
            else
            {
                // if there is an action to be taken, do it
                if (targetPhase.StartActionFromUp != null)
                {
                    bool performedStartFromUpActions = targetPhase.StartActionFromUp(director);
                    if (!performedStartFromUpActions)
                        return false;
                }
            }

            // Call the 'Both' action
            if (targetPhase.StartActionBoth != null)
            {
                bool performedCommonStartActions = targetPhase.StartActionBoth(director);
                if (!performedCommonStartActions)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Stops the current phase.
        /// </summary>
        /// <param name="director">The director.</param>
        /// <param name="targetPhase">The target phase.</param>
        /// <returns></returns>
        private static bool StopCurrentPhase(IImplantDirector director, DesignPhaseProperty targetPhase)
        {
            bool stoppedCurrentPhase = false;
            if (director.CurrentDesignPhaseProperty.StopAction != null)
            {
                stoppedCurrentPhase = director.CurrentDesignPhaseProperty.StopAction(director, targetPhase);
            }
            else
            {
                // No action required
                stoppedCurrentPhase = true;
            }
            
            // Show status
            if (stoppedCurrentPhase)
            {
                IDSPluginHelper.WriteLine(LogCategory.Default, string.Format("Successfully exited the {0} Phase.", director.CurrentDesignPhaseProperty.Name));
            }
            else
            {
                IDSPluginHelper.WriteLine(LogCategory.Default, string.Format("Unable to break out of the {0} Phase.", director.CurrentDesignPhaseProperty.Name));
            }

            return stoppedCurrentPhase;
        }        
    }
}