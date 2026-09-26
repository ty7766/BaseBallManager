using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// GameSession을 타석 단위로 자동 전진시켜 실시간 관전을 만든다.
/// 일시정지는 예약이라 진행 중인 타석이 끝난 뒤에 멈춘다
/// </summary>
public class GamePlaybackDriver : MonoBehaviour
{
    /// <summary>
    /// 타석 로그 1건 추가. 같은 타석의 도루 로그보다 뒤에 온다
    /// </summary>
    public event Action<SimulationBatterLog> BatterLogAdded;

    /// <summary>
    /// 도루 로그 1건 추가. 타석 로그보다 먼저 온다
    /// </summary>
    public event Action<SimulationStealLog> StealLogAdded;

    /// <summary>
    /// 예약한 일시정지가 실제로 걸렸다. 교체 화면을 여는 시점이다
    /// </summary>
    public event Action Paused;

    /// <summary>
    /// 경기 종료. 박스스코어로 넘어가는 시점이다
    /// </summary>
    public event Action Finished;

    public GameSession Session { get; private set; }

    public bool IsPlaying => _loop != null;
    public bool IsPaused { get; private set; }

    /// <summary>
    /// 일시정지를 눌렀지만 아직 타석이 끝나지 않은 상태. 버튼의 중간 표시에 쓴다
    /// </summary>
    public bool IsPauseRequested { get; private set; }

    [SerializeField, Tooltip("타석 하나를 보여주고 다음으로 넘어가기까지의 시간(초)")]
    private float _secondsPerAtBat = 1.2f;

    private Coroutine _loop;
    private WaitForSeconds _atBatDelay;

    /// <summary>
    /// 관전 시작. 이미 진행 중이거나 끝난 경기면 거절한다
    /// </summary>
    public bool Play(GameSession session)
    {
        if (session == null)
        {
            Debug.LogError("[GamePlaybackDriver]: 진행할 경기가 없습니다");
            return false;
        }

        if (IsPlaying)
        {
            Debug.LogWarning("[GamePlaybackDriver]: 이미 관전 중입니다");
            return false;
        }

        if (session.IsGameOver)
        {
            Debug.LogWarning("[GamePlaybackDriver]: 이미 끝난 경기입니다");
            return false;
        }

        Session = session;
        IsPaused = false;
        IsPauseRequested = false;
        _atBatDelay = new WaitForSeconds(Mathf.Max(0f, _secondsPerAtBat));
        _loop = StartCoroutine(PlayLoop());

        return true;
    }

    /// <summary>
    /// 일시정지 예약. 즉시 멈추지 않고 진행 중인 타석이 끝난 뒤에 멈춘다
    /// </summary>
    public bool RequestPause()
    {
        if (!IsPlaying || IsPaused || IsPauseRequested)
            return false;

        IsPauseRequested = true;
        return true;
    }

    /// <summary>
    /// 일시정지 해제
    /// </summary>
    public bool Resume()
    {
        if (!IsPaused)
            return false;

        IsPaused = false;
        return true;
    }

    /// <summary>
    /// 관전만 중단한다. 경기 상태는 그대로 남아 이어서 재생할 수 있다
    /// </summary>
    public void Stop()
    {
        if (_loop != null)
            StopCoroutine(_loop);

        _loop = null;
        IsPaused = false;
        IsPauseRequested = false;
    }

    //타석을 하나씩 전진시키며 로그를 흘린다
    private IEnumerator PlayLoop()
    {
        while (!Session.IsGameOver)
        {
            if (IsPaused)
            {
                yield return null;
                continue;
            }

            if (!Session.StepAtBat())
                break;

            PublishLastStep();

            yield return _atBatDelay;

            ApplyPauseRequest();
        }

        _loop = null;
        IsPaused = false;
        IsPauseRequested = false;

        Finished?.Invoke();
    }

    //이번 타석에서 새로 생긴 로그를 화면 순서대로 내보낸다
    private void PublishLastStep()
    {
        foreach (SimulationStealLog stealLog in Session.LastStepStealLogs)
            StealLogAdded?.Invoke(stealLog);

        if (Session.LastStepBatterLog != null)
            BatterLogAdded?.Invoke(Session.LastStepBatterLog);
    }

    //타석 경계에서만 실제로 멈춘다
    private void ApplyPauseRequest()
    {
        if (!IsPauseRequested)
            return;

        IsPauseRequested = false;
        IsPaused = true;

        Paused?.Invoke();
    }
}
