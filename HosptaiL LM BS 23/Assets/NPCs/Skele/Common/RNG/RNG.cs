using System;

namespace MH
{
    public class RNG
    {
        private uint m_w;
        private uint m_z;
        private uint m_seed1;
        private uint m_seed2;
        private int m_callCnt;

        public RNG()
        {
            m_seed1 = (m_w = 521288629u);
            m_seed2 = (m_z = 362436069u);
        }
        public void SetSeed(uint u, uint v)
        {
            if (u != 0u)
            {
                m_w = u;
                m_seed1 = u;
            }
            if (v != 0u)
            {
                m_z = v;
                m_seed2 = v;
            }
            m_callCnt = 0;
        }
        public void SetSeed(uint u)
        {
            SetSeed(u, m_seed2);
        }
        public void SetSeedFromSystemTime()
        {
            long num = DateTime.Now.ToFileTime();
            SetSeed((uint)(num >> 16), (uint)(num % 4294967296L));
        }
        public uint GetSeed1()
        {
            return m_seed1;
        }
        public uint GetSeed2()
        {
            return m_seed2;
        }
        public int DBG_CallCnt()
        {
            return m_callCnt;
        }
        public double GetUniform()
        {
            uint v = GetUint();
            return (v + 1.0) * 2.3283064354544941E-10;
        }
        public uint GetUint()
        {
            m_callCnt++;
            m_z = 36969u * (m_z & 65535u) + (m_z >> 16);
            m_w = 18000u * (m_w & 65535u) + (m_w >> 16);
            return (m_z << 16) + m_w;
        }
        public double GetNormal()
        {
            double uniform = GetUniform();
            double uniform2 = GetUniform();
            double num = Math.Sqrt(-2.0 * Math.Log(uniform));
            double a = 6.2831853071795862 * uniform2;
            return num * Math.Sin(a);
        }
        public double GetNormal(double mean, double standardDeviation)
        {
            if (standardDeviation <= 0.0)
            {
                string paramName = string.Format("Shape must be positive. Received {0}.", standardDeviation);
                throw new ArgumentOutOfRangeException(paramName);
            }
            return mean + standardDeviation * GetNormal();
        }
        public double GetExponential()
        {
            return -Math.Log(GetUniform());
        }
        public double GetExponential(double mean)
        {
            if (mean <= 0.0)
            {
                string paramName = string.Format("Mean must be positive. Received {0}.", mean);
                throw new ArgumentOutOfRangeException(paramName);
            }
            return mean * GetExponential();
        }
        public double GetGamma(double shape, double scale)
        {
            if (shape >= 1.0)
            {
                double num = shape - 0.33333333333333331;
                double num2 = 1.0 / Math.Sqrt(9.0 * num);
                double num3;
                double uniform;
                double num4;
                do
                {
                    double normal;
                    do
                    {
                        normal = GetNormal();
                        num3 = 1.0 + num2 * normal;
                    }
                    while (num3 <= 0.0);
                    num3 = num3 * num3 * num3;
                    uniform = GetUniform();
                    num4 = normal * normal;
                }
                while (uniform >= 1.0 - 0.0331 * num4 * num4 && Math.Log(uniform) >= 0.5 * num4 + num * (1.0 - num3 + Math.Log(num3)));
                return scale * num * num3;
            }
            if (shape <= 0.0)
            {
                string paramName = string.Format("Shape must be positive. Received {0}.", shape);
                throw new ArgumentOutOfRangeException(paramName);
            }
            double gamma = GetGamma(shape + 1.0, 1.0);
            double uniform2 = GetUniform();
            return scale * gamma * Math.Pow(uniform2, 1.0 / shape);
        }
        public double GetChiSquare(double degreesOfFreedom)
        {
            return GetGamma(0.5 * degreesOfFreedom, 2.0);
        }
        public double GetInverseGamma(double shape, double scale)
        {
            return 1.0 / GetGamma(shape, 1.0 / scale);
        }
        public double GetWeibull(double shape, double scale)
        {
            if (shape <= 0.0 || scale <= 0.0)
            {
                string paramName = string.Format("Shape and scale parameters must be positive. Recieved shape {0} and scale{1}.", shape, scale);
                throw new ArgumentOutOfRangeException(paramName);
            }
            return scale * Math.Pow(-Math.Log(GetUniform()), 1.0 / shape);
        }
        public double GetCauchy(double median, double scale)
        {
            if (scale <= 0.0)
            {
                string message = string.Format("Scale must be positive. Received {0}.", scale);
                throw new ArgumentException(message);
            }
            double uniform = GetUniform();
            return median + scale * Math.Tan(3.1415926535897931 * (uniform - 0.5));
        }
        public double GetStudentT(double degreesOfFreedom)
        {
            if (degreesOfFreedom <= 0.0)
            {
                string message = string.Format("Degrees of freedom must be positive. Received {0}.", degreesOfFreedom);
                throw new ArgumentException(message);
            }
            double normal = GetNormal();
            double chiSquare = GetChiSquare(degreesOfFreedom);
            return normal / Math.Sqrt(chiSquare / degreesOfFreedom);
        }
        public double GetLaplace(double mean, double scale)
        {
            double uniform = GetUniform();
            return (uniform >= 0.5) ? (mean - scale * Math.Log(2.0 * (1.0 - uniform))) : (mean + scale * Math.Log(2.0 * uniform));
        }
        public double GetLogNormal(double mu, double sigma)
        {
            return Math.Exp(GetNormal(mu, sigma));
        }
        public double GetBeta(double a, double b)
        {
            if (a <= 0.0 || b <= 0.0)
            {
                string paramName = string.Format("Beta parameters must be positive. Received {0} and {1}.", a, b);
                throw new ArgumentOutOfRangeException(paramName);
            }
            double gamma = GetGamma(a, 1.0);
            double gamma2 = GetGamma(b, 1.0);
            return gamma / (gamma + gamma2);
        }

        // [min, max]
        public float Range(float min, float max)
        {
            float p = (float)GetUniform() * (max - min);
            return p + min;
        }

        // [min, max)
        public int Range(int min, int max)
        {
            int r = (int) (GetUint() % (max - min));
            return min + r;
        }
    }

    /// <summary>
    /// core RNG, used for network sync
    /// </summary>
    public class CoreRNG : RNG
    {
        public static CoreRNG Instance = new CoreRNG();
    }

}


