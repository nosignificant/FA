using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 레벨 전체 진행도(활성화된 방 L+A 수 / 전체)에 비례해 지정한 Light들의 밝기를 올린다.
//  - 진행도 0  → 각 조명의 원래 밝기 × dimFactor (어둑)
//  - 진행도 1  → 각 조명의 원래 밝기 (풀)
//  방 상태가 바뀔 때마다 자동 갱신. (LevelProgressMeter / WallActivationTint와 동일한 구독 패턴)
//  각 Light의 '시작 intensity'를 최대값으로 기억하므로, 인스펙터에서 밝기를 조절하면 그게 100% 기준이 됨.
public class ProgressLightBrightness : MonoBehaviour
{
    [Tooltip("진행도에 따라 밝아질 조명들. 비우면 이 오브젝트+자식의 모든 Light 자동 수집")]
    public Light[] lights;

    [Tooltip("진행도가 꽉 차는 기준 활성 방 수. 0이면 구독된(대상) 방 전체 수")]
    public int fullAtTotal = 0;

    [Tooltip("이 씬(레벨)의 방만 셀지. 끄면 로드된 전체 방 합계(엔딩 판정 기준과 동일)")]
    public bool thisSceneOnly = true;

    [Range(0f, 1f)]
    [Tooltip("진행도 0일 때 밝기 배수 (원래 밝기 대비). 0=완전 꺼짐, 1=처음부터 풀밝기")]
    public float dimFactor = 0.15f;

    [Tooltip("밝기 변화 속도. 0이면 즉시, 크면 빠르게 보간 (예: 2 = 부드럽게 서서히)")]
    public float smoothSpeed = 2f;

    private Light[] _lights;
    private float[] _baseIntensity;   // 각 Light의 원래(최대) 밝기
    private Room[] _subscribed;
    private float _curT;              // 현재 적용 중인 진행도(보간값)
    private float _tgtT;              // 목표 진행도

    private Light[] Lights()
    {
        if (_lights != null) return _lights;
        _lights = (lights != null && lights.Length > 0)
            ? lights
            : GetComponentsInChildren<Light>(true);
        _baseIntensity = new float[_lights.Length];
        for (int i = 0; i < _lights.Length; i++)
            _baseIntensity[i] = _lights[i] != null ? _lights[i].intensity : 0f;
        return _lights;
    }

    // area SetActive(false)로 껐다 켜면 코루틴/구독이 끊긴다 → OnEnable에서 다시 바인딩.
    private void OnEnable() => StartCoroutine(Bind());

    private void OnDisable()
    {
        if (_subscribed == null) return;
        foreach (var r in _subscribed)
            if (r != null) r.OnActivationChanged -= OnRoomChanged;
        _subscribed = null;
    }

    // RoomManager 준비 + 같은 씬 방 등록을 기다렸다가 구독하고 초기 밝기 적용.
    private IEnumerator Bind()
    {
        while (RoomManager.Instance == null || RoomManager.Instance.allRooms == null)
            yield return null;
        yield return null;   // 같은 씬 방들이 모두 등록될 시간을 한 프레임 줌

        var list = new List<Room>();
        foreach (var r in RoomManager.Instance.allRooms)
        {
            if (r == null) continue;
            if (thisSceneOnly && r.gameObject.scene != gameObject.scene) continue;
            r.OnActivationChanged -= OnRoomChanged;   // 중복 구독 방지
            r.OnActivationChanged += OnRoomChanged;
            list.Add(r);
        }
        _subscribed = list.ToArray();

        _tgtT = ComputeProgress();
        _curT = _tgtT;          // 씬 진입 시엔 즉시 현재 진행도로 (페이드 없이 시작)
        ApplyIntensity(_curT);
    }

    private void OnRoomChanged(Room.RoomActivation _) => _tgtT = ComputeProgress();

    // 활성화된 방(L+A) 수 / 기준치 → 0~1
    private float ComputeProgress()
    {
        if (RoomManager.Instance == null) return 0f;
        int activated = 0, total = 0;
        foreach (var r in RoomManager.Instance.allRooms)
        {
            if (r == null) continue;
            if (thisSceneOnly && r.gameObject.scene != gameObject.scene) continue;
            total++;
            if (r.Activation == Room.RoomActivation.L || r.Activation == Room.RoomActivation.A)
                activated++;
        }
        int full = fullAtTotal > 0 ? fullAtTotal : total;
        if (full <= 0) return 0f;
        return Mathf.Clamp01(activated / (float)full);
    }

    private void Update()
    {
        if (_lights == null) return;
        if (Mathf.Approximately(_curT, _tgtT)) return;   // 변화 없으면 쉬기

        _curT = smoothSpeed > 0f
            ? Mathf.MoveTowards(_curT, _tgtT, smoothSpeed * Time.deltaTime)
            : _tgtT;
        ApplyIntensity(_curT);
    }

    // 진행도 t(0~1)에 맞춰 각 조명 밝기 = 원래밝기 × lerp(dimFactor, 1, t)
    private void ApplyIntensity(float t)
    {
        var ls = Lights();
        float mul = Mathf.Lerp(dimFactor, 1f, t);
        for (int i = 0; i < ls.Length; i++)
            if (ls[i] != null) ls[i].intensity = _baseIntensity[i] * mul;
    }
}
