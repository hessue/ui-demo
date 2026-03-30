using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

namespace BlockAndDagger.Utils
{
    public static class AsyncUtilities
    {
        /// <summary>
        /// Waits for the given Task to complete by yielding each frame. If the task
        /// faults, the exception is logged and the optional onException callback is invoked.
        /// </summary>
        public static IEnumerator WaitForTask(Task task, Action<Exception> onException = null)
        {
            if (task == null)
                yield break;

            while (!task.IsCompleted)
            {
                yield return null;
            }

            if (task.IsFaulted)
            {
                Debug.LogException(task.Exception);
                onException?.Invoke(task.Exception);
            }
        }
    }
}

