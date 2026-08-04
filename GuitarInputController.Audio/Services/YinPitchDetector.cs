using GuitarInputController.Audio.Interfaces;
using GuitarInputController.Audio.Models;
using GuitarInputController.Core.Constants;

namespace GuitarInputController.Audio.Services;

/// <summary>
/// YIN 算法音高检测器实现
/// 参考: De Cheveigné, A., & Kawahara, H. (2002). "YIN, a fundamental frequency estimator for speech and music."
/// </summary>
public class YinPitchDetector : IPitchDetector
{
    private readonly float _yinThreshold;
    private readonly int _minFrequency;
    private readonly int _maxFrequency;

    /// <summary>置信度阈值</summary>
    public double ConfidenceThreshold { get; set; } = 0.7;

    /// <summary>音量阈值</summary>
    public double VolumeThreshold { get; set; } = 0.05;

    /// <param name="yinThreshold">YIN 算法阈值（默认 0.15），越低越严格</param>
    /// <param name="minFrequency">最低检测频率（Hz），默认 65Hz（约 C2）</param>
    /// <param name="maxFrequency">最高检测频率（Hz），默认 1500Hz（约 F#6）</param>
    public YinPitchDetector(float yinThreshold = 0.15f, int minFrequency = 65, int maxFrequency = 1500)
    {
        _yinThreshold = yinThreshold;
        _minFrequency = minFrequency;
        _maxFrequency = maxFrequency;
    }

    public PitchResult? DetectPitch(float[] samples, int sampleRate)
    {
        if (samples.Length == 0) return null;

        // 1. 检查音量阈值
        float rms = CalculateRms(samples);
        if (rms < VolumeThreshold)
            return null;

        // 2. 确定搜索范围（以采样数为单位）
        int minPeriod = sampleRate / _maxFrequency;
        int maxPeriod = sampleRate / _minFrequency;

        // 确保至少有 2 倍最大周期长度的缓冲区用于计算
        int minBufferSize = 2 * maxPeriod;
        if (samples.Length < minBufferSize)
            return null;

        // 实际分析长度 = min(samples.Length, 2 * maxPeriod)
        int analysisLength = Math.Min(samples.Length, minBufferSize);

        // 3. YIN 步骤 1: 计算差分函数 d_t(τ)
        float[] diffFunc = ComputeDifferenceFunction(samples, analysisLength, maxPeriod);

        // 4. YIN 步骤 2: 计算累积均值归一化差分函数 d'_t(τ)
        float[] cmndf = ComputeCumulativeMeanNormalizedDiff(diffFunc);

        // 5. YIN 步骤 3: 寻找第一个低于阈值的极小值
        int periodIndex = FindFirstMinimumBelowThreshold(cmndf, minPeriod, maxPeriod);
        if (periodIndex < 0)
            return null;

        // 6. YIN 步骤 4: 抛物线插值以获得更高精度
        double interpolatedPeriod = ParabolicInterpolation(cmndf, periodIndex);

        // 7. 计算频率
        double frequency = sampleRate / interpolatedPeriod;

        // 验证频率是否在合理范围内
        if (frequency < _minFrequency || frequency > _maxFrequency)
            return null;

        // 8. 计算置信度
        double confidence = CalculateConfidence(cmndf, periodIndex);

        if (confidence < ConfidenceThreshold)
            return null;

        // 9. 解析为音名和八度
        int midiNumber = NoteConstants.FrequencyToMidi(frequency);
        if (midiNumber < 0 || midiNumber > 127)
            return null;

        var (noteName, octave) = NoteConstants.MidiToNote(midiNumber);

        return new PitchResult
        {
            Frequency = frequency,
            NoteName = noteName,
            Octave = octave,
            MidiNoteNumber = midiNumber,
            Confidence = confidence,
            Volume = rms
        };
    }

    /// <summary>
    /// 计算 RMS 音量级别
    /// </summary>
    private static float CalculateRms(float[] samples)
    {
        float sum = 0;
        for (int i = 0; i < samples.Length; i++)
            sum += samples[i] * samples[i];
        return MathF.Sqrt(sum / samples.Length);
    }

    /// <summary>
    /// YIN 步骤 1: 计算差分函数
    /// d_t(τ) = Σ (x_j - x_{j+τ})²
    /// </summary>
    private static float[] ComputeDifferenceFunction(float[] samples, int analysisLength, int maxPeriod)
    {
        float[] diff = new float[maxPeriod + 1];

        for (int tau = 0; tau <= maxPeriod; tau++)
        {
            float sum = 0;
            for (int j = 0; j < analysisLength - maxPeriod; j++)
            {
                float delta = samples[j] - samples[j + tau];
                sum += delta * delta;
            }
            diff[tau] = sum;
        }

        return diff;
    }

    /// <summary>
    /// YIN 步骤 2: 累积均值归一化差分函数
    /// d'_t(τ) = d_t(τ) / [(1/τ) * Σ d_t(j)]  （对于 τ = 0 的特殊情况，设 d'_t(0) = 1）
    /// </summary>
    private static float[] ComputeCumulativeMeanNormalizedDiff(float[] diffFunc)
    {
        int length = diffFunc.Length;
        float[] cmndf = new float[length];

        cmndf[0] = 1.0f;
        float runningSum = 0;

        for (int tau = 1; tau < length; tau++)
        {
            runningSum += diffFunc[tau];
            if (runningSum > 0)
                cmndf[tau] = diffFunc[tau] * tau / runningSum;
            else
                cmndf[tau] = 1.0f;
        }

        return cmndf;
    }

    /// <summary>
    /// YIN 步骤 3: 寻找第一个低于阈值的极小值
    /// 如果值低于阈值且小于前一个值，则为极小值的起始点
    /// </summary>
    private int FindFirstMinimumBelowThreshold(float[] cmndf, int minPeriod, int maxPeriod)
    {
        int tau = minPeriod;
        while (tau < maxPeriod)
        {
            if (cmndf[tau] < _yinThreshold)
            {
                // 确保这是一个局部极小值（比前后都小）
                while (tau + 1 < maxPeriod && cmndf[tau + 1] < cmndf[tau])
                    tau++;

                return tau;
            }
            tau++;
        }
        return -1;
    }

    /// <summary>
    /// YIN 步骤 4: 抛物线插值
    /// 使用相邻三个点进行抛物线插值以获得亚采样精度
    /// </summary>
    private static double ParabolicInterpolation(float[] cmndf, int tau)
    {
        if (tau <= 0 || tau >= cmndf.Length - 1)
            return tau;

        float before = cmndf[tau - 1];
        float current = cmndf[tau];
        float after = cmndf[tau + 1];

        float denominator = before - 2 * current + after;
        if (Math.Abs(denominator) < 1e-10f)
            return tau;

        float correction = (before - after) / (2 * denominator);
        return tau + correction;
    }

    /// <summary>
    /// 计算检测置信度
    /// 基于 YIN 函数在极小值处的凹陷深度 — 凹陷越深，置信度越高
    /// </summary>
    private static double CalculateConfidence(float[] cmndf, int periodIndex)
    {
        if (periodIndex <= 0 || periodIndex >= cmndf.Length)
            return 0;

        // 置信度 = 1 - 极小值（深谷 = 高置信度）
        float valleyDepth = cmndf[periodIndex];

        // 同时检查该极小值比周围的值低多少
        float localContrast = 0;
        int range = Math.Min(5, cmndf.Length - periodIndex - 1);
        for (int i = 1; i <= range; i++)
        {
            localContrast = Math.Max(localContrast, cmndf[periodIndex + i] - valleyDepth);
            if (periodIndex - i >= 0)
                localContrast = Math.Max(localContrast, cmndf[periodIndex - i] - valleyDepth);
        }

        // 结合凹陷深度和局部对比度
        double depthScore = 1.0 - Math.Clamp(valleyDepth, 0, 1);
        double contrastScore = Math.Clamp(localContrast / 2.0, 0, 1);

        return (depthScore * 0.6 + contrastScore * 0.4);
    }
}
