using UnityEngine;

// Tentacle(v1 FABRIK)·Tentacle2(물리 스프링) 공통 베이스.
// TentacleGrab2·TargetControl 등은 이 타입으로 촉수를 다뤄 두 버전 모두 호환.
//  - Foot  : 잡은 생물을 붙이는 끝단 Transform
//  - Target: 촉수가 조준(뻗어갈)할 대상. 각 구현이 자기 필드로 노출.
public abstract class TentacleBase : MonoBehaviour
{
    public abstract Transform Foot { get; }
    public abstract Transform Target { get; set; }
}
