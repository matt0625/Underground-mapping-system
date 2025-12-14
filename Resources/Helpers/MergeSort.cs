using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TUBETREKWPFV1.Classes;

namespace TUBETREKWPFV1.Resources.Helpers
{
    internal class MergeSort
    {
        public static List<UserJourneyRetrieved> Mergesort(List<UserJourneyRetrieved> Unsorted)
        {
            // recursive base case: return the list if cant be divided down any more
            if (Unsorted.Count <= 1)
            {
                return Unsorted;
            }

            // otherwise divide unsorted list into 2 halves
            List<UserJourneyRetrieved> Left = new List<UserJourneyRetrieved>();
            List<UserJourneyRetrieved> Right = new List<UserJourneyRetrieved>();

            int Mid = Unsorted.Count / 2;
            // add the left half to left list
            for (int i = 0; i < Mid;  i++)
            {
                Left.Add(Unsorted[i]);
            }
            // add the right half to right list
            for (int i = Mid; i < Unsorted.Count; i++)
            {
                Right.Add(Unsorted[i]);
            }


            // divide again until base case reached then merge each side
            Left = Mergesort(Left);
            Right = Mergesort(Right);

            return Merge(Left, Right);
        }

        private static List<UserJourneyRetrieved> Merge(List<UserJourneyRetrieved> Left, List<UserJourneyRetrieved> Right)
        {
            List<UserJourneyRetrieved> EndResult = new List<UserJourneyRetrieved>();

            // begin comparing individual elements to add to the new sorted list above ^
            while (Left.Count > 0 || Right.Count > 0)
            {
                // if both are still populated we must do a direct comparison of values
                if (Left.Count > 0 && Right.Count > 0)
                {
                    // in this case we sort by how recent the journey was searched for
                    // an older datetime is less than a newer one, so here we are essentially sorting oldest to newest
                    if (Left.First().DateTime <= Right.First().DateTime)
                    {
                        EndResult.Add(Left.First());
                        Left.Remove(Left.First());
                    }
                    else
                    {
                        EndResult.Add(Right.First());
                        Right.Remove(Right.First());
                    }
                }
                // if we've sorted all the right values, we can just add the left values in order as they are already sorted from the recursive process starting with comparing single-element lists
                else if (Left.Count > 0)
                {
                    EndResult.Add(Left.First());
                    Left.Remove(Left.First());
                }
                // likewise for the right
                else if (Right.Count > 0)
                {
                    EndResult.Add(Right.First());
                    Right.Remove(Right.First());
                }
            }
            return EndResult;
        }
    }
}
