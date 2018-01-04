using System;
using System.Collections.Generic;

using Job = System.Collections.IEnumerator;

namespace MH.Skele
{
    /// <summary>
    /// a tailor-ed version Coroutine container for Skele
    /// </summary>
    public class Skele_CRCont
    {
        #region "data"
        // data
        private List<Job> m_RunningJobs = new List<Job>();
        private Dictionary<Job, Job> m_waitExecs = new Dictionary<Job, Job>(); //<doing, waiting>, when a job is finished, check against this map, if has entry, put the tvalue back to m_RunningJobs

        private List<Job> m_toAddTasks = new List<Job>();
        private List<Job> m_toDelTasks = new List<Job>();

        #endregion

        #region "public method"
        // public method

        public Skele_CRCont()
        {
        }

        /// <summary>
        /// start a new coroutine, 
        /// this coroutine will be executed immediately until the first yield return;
        /// if calling code yield return on the returned value, the calling code will be suspended until `work' is finished
        /// </summary>
        public WaitJob Start(Job work)
        {
            work.MoveNext();

            m_toAddTasks.Add(work);

            WaitJob wjob = new WaitJob(work);

            _CheckJobReturn(work);

            return wjob;
        }

        /// <summary>
        /// clear all queues, 
        /// un-sub all events
        /// </summary>
        public void Clear()
        {
            m_RunningJobs.Clear();
            m_waitExecs.Clear();
            m_toAddTasks.Clear();
        }

        /// <summary>
        /// execute reg-ed jobs
        /// </summary>
        public void Execute()
        {
            //////////////////////////////////////////////////////////////////////////
            // 1. find any works in frameTasks is eligible for execute, add it back to m_tasks;
            // 2. execute m_tasks, if any task is done, check if it's in m_waitExec
            //////////////////////////////////////////////////////////////////////////

            // step 1
            {
            }

            //step 2
            {
                for (var ie = m_RunningJobs.GetEnumerator(); ie.MoveNext(); )
                {
                    var task = ie.Current;
                    bool bNotOver = task.MoveNext();
                    if (bNotOver)
                    {
                        _CheckJobReturn(task);
                    }
                    else
                    {
                        m_toDelTasks.Add(task);
                        if (m_waitExecs.ContainsKey(task))
                        {
                            Job toResume = m_waitExecs[task];
                            m_toAddTasks.Add(toResume);
                        }
                    }
                } //foreach(Job task in m_RunningJobs)

                for (var ie = m_toAddTasks.GetEnumerator(); ie.MoveNext(); )
                {
                    //Dbg.Log("add task");
                    var task = ie.Current;
                    m_RunningJobs.Add(task);
                }
                for (var ie = m_toDelTasks.GetEnumerator(); ie.MoveNext(); )
                {
                    //Dbg.Log("remove task");
                    var task = ie.Current;
                    m_RunningJobs.Remove(task);
                }

            } //step 2

            m_toDelTasks.Clear();
            m_toAddTasks.Clear();

        }

        /// <summary>
        /// check the coroutine return value,
        /// according to the returned value, current coroutine might be suspended temporarily
        /// </summary>
        private void _CheckJobReturn(Job task)
        {
            object ret = task.Current;

            Yielder yd = ret as Yielder;
            if (yd != null)
            {
                switch (yd.m_type)
                {
                    case Yielder.TYPE.WAIT_JOB_DONE:
                        {
                            WaitJob wuc = (WaitJob)yd;

                            m_waitExecs.Add(wuc.m_work, task); //current task will be suspended until given new task is finished
                            //m_toAddTasks.Add(wuc.m_work); 
                            m_toDelTasks.Add(task); //current task is suspended
                        }
                        break;
                }
            }
        }
        #endregion

        #region "private method"
        // private method

        #endregion

        #region "constant data"
        // constant data

        #endregion

        #region "inner type"
        // "inner type" 

        #endregion

    }


    /// <summary>
    /// the base class of yield instructions
    /// </summary>
    public class Yielder
    {
        public enum TYPE { WAIT_FRAME, WAIT_JOB_DONE, WAIT_EVT };
        public TYPE m_type;
        public Yielder(TYPE t) { m_type = t; }
    }

    /// <summary>
    /// wait for a job done
    /// </summary>
    public class WaitJob : Yielder
    {
        public Job m_work;

        public WaitJob(Job task)
            : base(Yielder.TYPE.WAIT_JOB_DONE)
        {
            m_work = task;
        }
    }

}