#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
NAICR 경제 밸런스 시뮬레이션
게임 내 경제 시스템의 밸런스를 검증하는 시뮬레이션
"""

import random
import matplotlib.pyplot as plt
import matplotlib.font_manager as fm
import os
import json
from dataclasses import dataclass
from typing import Dict, List, Tuple, Optional

# 시드 고정으로 재현 가능하게
random.seed(42)

# 한글 폰트 설정
plt.rcParams['font.family'] = ['DejaVu Sans', 'SimHei', 'Malgun Gothic', 'AppleGothic']
plt.rcParams['axes.unicode_minus'] = False

@dataclass
class GameStats:
    """게임 통계 클래스"""
    money: float = 0  # 소지금
    hp: int = 70      # 체력
    san: int = 60     # 정신력
    yanggi: int = 50  # 양기 (적정 구간 30~70)
    satiety: int = 50 # 포만감
    day: int = 1      # 경과 일수
    unpaid_rent_count: int = 0  # 월세 미납 횟수
    has_vr: bool = False  # VR 소유 여부
    work_days_this_week: int = 0  # 이번 주 출근일수
    consecutive_frozen_food_days: int = 0  # 냉동식품 연속일수
    last_fear_event_turn: int = -10  # 마지막 공포 이벤트 턴

@dataclass
class SimulationConfig:
    """시뮬레이션 설정"""
    name: str
    work_pattern: str  # "5days", "5days_weekend", "3days", "4days"
    food_pattern: str  # "frozen_only", "frozen_delivery", "mixed"
    vr_purchase_day: Optional[int] = None  # VR 구매일 (None이면 구매 안함)
    difficulty: str = "normal"  # "easy", "normal", "hard"

class NAICRSimulator:
    """NAICR 경제 시뮬레이션"""
    
    def __init__(self):
        # 기본 설정값들
        self.WEEKDAY_SALARY = 80000
        self.OVERTIME_SALARY = 130000
        self.WEEKEND_SALARY = 130000
        self.TRANSPORT_COST = 5000
        self.VR_PRICE = 440000
        
        # 고정지출 (월 단위)
        self.MONTHLY_EXPENSES = {
            "rent": 250000,
            "loan_interest": 120000,
            "student_loan": 100000,
            "utilities": 90000,
            "electricity": 50000,
            "internet": 25000,
            "phone": 35000,
            "water_rental": 25000,
            "insurance": 90000,
            "ott": 15000,
            "game_sub": 20000,
            "cigarettes": 67500,
            "coffee": 45000,
            "appliance_installment": 347000,  # 10개월간만
            "hygiene": 30000
        }
        
        # 식비 설정 (cost, hp_gain, satiety_gain, yanggi_gain)
        self.FOOD_OPTIONS = {
            "frozen": (2500, 4, 30, 1),
            "convenience": (4000, 5, 35, 1),
            "delivery": (12000, 8, 40, 1),
            "premium_delivery": (25000, 12, 45, 4)  # 양기+3 -> 식사+1 포함하여 4
        }
        
        self.results = {}
        
    def calculate_monthly_expenses(self, day: int, difficulty: str) -> float:
        """월별 고정지출 계산"""
        total = sum(self.MONTHLY_EXPENSES.values())
        
        # 11개월째부터 가전 할부 제거
        if day > 10 * 28:  # 10개월 = 280일
            total -= self.MONTHLY_EXPENSES["appliance_installment"]
        
        # 난이도별 조정
        if difficulty == "easy":
            total *= 0.8  # -20%
        elif difficulty == "hard":
            total *= 1.1  # +10%
            
        return total / 28  # 일당으로 변환
    
    def apply_yanggi_penalties(self, stats: GameStats) -> None:
        """양기 수치에 따른 페널티 적용"""
        if stats.yanggi >= 80:
            if stats.yanggi >= 100:
                stats.san -= 3  # 80~100: SAN -3/일
            else:
                stats.san -= 1  # 70~80: SAN -1/일
        elif stats.yanggi <= 30:
            if stats.yanggi <= 20:
                stats.hp -= 4  # 0~20: HP -4/일
            else:
                stats.hp -= 1  # 20~30: HP -1/일
    
    def apply_satiety_decay(self, stats: GameStats, is_sleeping: bool = False) -> None:
        """포만감 자연 감소"""
        if stats.satiety > 80:
            decay = 3
        elif stats.satiety > 40:
            decay = 8
        elif stats.satiety > 20:
            decay = 6.7
        else:
            decay = 2
            
        if is_sleeping:
            decay *= 0.5
            
        stats.satiety = max(0, stats.satiety - decay)
    
    def process_sleep(self, stats: GameStats) -> None:
        """수면 처리"""
        # 포만감에 따른 HP 회복률 조정
        hp_recovery = 35
        if stats.satiety < 20:
            hp_recovery *= 0.3
        elif stats.satiety < 40:
            hp_recovery *= 0.5
            
        stats.hp = min(100, stats.hp + hp_recovery)
        stats.san = min(100, stats.san + 12)
        stats.yanggi = min(100, stats.yanggi + 5)
        
        # 양기 80 이상일 때 몽정 처리
        if stats.yanggi >= 80:
            stats.yanggi -= 12
            stats.san -= 6
            
        # 수면 중 포만감 감소
        self.apply_satiety_decay(stats, is_sleeping=True)
    
    def check_fear_event(self, stats: GameStats, turn: int, difficulty: str) -> bool:
        """공포 이벤트 발생 체크"""
        # 최근 1턴에 발생했으면 0%
        if stats.last_fear_event_turn == turn - 1:
            return False
            
        # 기본 확률 계산
        if stats.day <= 3:
            base_chance = 0.04  # 3~5%의 중간값
        elif stats.day <= 6:
            base_chance = 0.10  # 8~12%의 중간값
        else:
            base_chance = 0.175  # 15~20%의 중간값
            
        # 밤 시간 (턴 8 이후) 2배
        if turn >= 8:
            base_chance *= 2
            
        # 최근 3턴 미발생시 +5%
        if turn - stats.last_fear_event_turn > 3:
            base_chance += 0.05
            
        # 난이도 조정
        if difficulty == "easy":
            base_chance *= 0.7  # -30%
        elif difficulty == "hard":
            base_chance *= 1.3  # +30%
            
        if random.random() < base_chance:
            stats.last_fear_event_turn = turn
            # 공포 이벤트 효과
            stats.hp -= random.randint(0, 2)
            stats.san -= random.randint(8, 15)
            return True
            
        return False
    
    def simulate_work_day(self, stats: GameStats, is_overtime: bool, difficulty: str) -> float:
        """출근일 시뮬레이션"""
        earnings = 0
        
        if is_overtime:
            earnings = self.OVERTIME_SALARY
            stats.hp -= 15  # 야근 HP 소모
            stats.san -= 8   # 야근 SAN 소모
        else:
            earnings = self.WEEKDAY_SALARY
            stats.hp -= 10  # 출근 HP 소모
            stats.san -= 4   # 출근 SAN 소모
            
        # 교통비 차감
        earnings -= self.TRANSPORT_COST
        
        # 양기 소모 난이도 조정
        yanggi_cost = 2
        if difficulty == "easy":
            yanggi_cost *= 0.7
        elif difficulty == "hard":
            yanggi_cost *= 1.3
            
        stats.yanggi -= yanggi_cost
        stats.work_days_this_week += 1
        
        return earnings
    
    def simulate_weekend_work(self, stats: GameStats) -> float:
        """주말 출근 시뮬레이션"""
        stats.hp -= 10
        stats.san -= 4
        stats.yanggi -= 2
        stats.work_days_this_week += 1
        return self.WEEKEND_SALARY - self.TRANSPORT_COST
    
    def eat_meal(self, stats: GameStats, food_type: str, difficulty: str) -> float:
        """식사 처리"""
        cost, hp_gain, satiety_gain, yanggi_gain = self.FOOD_OPTIONS[food_type]
        
        stats.hp = min(100, stats.hp + hp_gain)
        stats.satiety = min(100, stats.satiety + satiety_gain)
        
        # 양기 조정 (난이도 반영)
        if difficulty == "easy":
            yanggi_gain *= 1.3
        elif difficulty == "hard":
            yanggi_gain *= 0.7
            
        stats.yanggi = min(100, stats.yanggi + yanggi_gain)
        
        # 냉동식품 연속 섭취 체크
        if food_type == "frozen":
            stats.consecutive_frozen_food_days += 1
            if stats.consecutive_frozen_food_days >= 3:
                # 배탈 발생
                stats.hp -= 10
                stats.satiety = 0
                stats.consecutive_frozen_food_days = 0
        else:
            stats.consecutive_frozen_food_days = 0
            
        return cost
    
    def choose_food(self, food_pattern: str, day: int, money: float) -> str:
        """식사 패턴에 따른 음식 선택"""
        if food_pattern == "frozen_only":
            return "frozen"
        elif food_pattern == "frozen_delivery":
            # 가끔 배달 (10일에 1번)
            if day % 10 == 0 and money > 15000:
                return "delivery"
            return "frozen"
        elif food_pattern == "mixed":
            # 다양한 음식 섭취
            if money > 30000 and day % 5 == 0:
                return "premium_delivery"
            elif money > 15000 and day % 3 == 0:
                return "delivery"
            elif money > 5000:
                return "convenience"
            else:
                return "frozen"
        return "frozen"
    
    def use_vr(self, stats: GameStats) -> None:
        """VR 사용 (자위)"""
        stats.san += 3
        stats.yanggi -= 3.5
    
    def simulate_scenario(self, config: SimulationConfig, days: int = 70) -> Dict:
        """시나리오 시뮬레이션"""
        stats = GameStats()
        
        # 결과 저장용 리스트들
        daily_money = []
        daily_hp = []
        daily_san = []
        daily_yanggi = []
        daily_satiety = []
        events = []
        
        # 초기 소지금 설정 (첫 달 생존 가능한 수준)
        stats.money = 1500000
        
        for day in range(1, days + 1):
            stats.day = day
            daily_expenses = 0
            daily_earnings = 0
            
            # 월말 고정지출 처리 (28일마다)
            if day % 28 == 0:
                monthly_expense = self.calculate_monthly_expenses(day, config.difficulty)
                monthly_total = monthly_expense * 28
                stats.money -= monthly_total
                daily_expenses += monthly_total
                
                # 월세 미납 체크
                if stats.money < 0:
                    stats.unpaid_rent_count += 1
                    if stats.unpaid_rent_count >= 3:
                        events.append(f"Day {day}: 게임 오버 (월세 3회 미납)")
                        break
            
            # 주급 정산 (7일마다)
            if day % 7 == 0 and stats.work_days_this_week > 0:
                weekly_earnings = stats.work_days_this_week * self.WEEKDAY_SALARY
                stats.money += weekly_earnings
                daily_earnings += weekly_earnings
                stats.work_days_this_week = 0
            
            # VR 구매 체크
            if config.vr_purchase_day and day == config.vr_purchase_day and not stats.has_vr:
                if stats.money >= self.VR_PRICE:
                    stats.money -= self.VR_PRICE
                    stats.has_vr = True
                    daily_expenses += self.VR_PRICE
                    events.append(f"Day {day}: VR 구매")
            
            # 하루 시뮬레이션 (10턴)
            for turn in range(10):
                # 공포 이벤트 체크
                if self.check_fear_event(stats, turn, config.difficulty):
                    events.append(f"Day {day} Turn {turn}: 공포 이벤트 발생")
                
                # 출근 패턴에 따른 행동
                is_workday = False
                if config.work_pattern == "5days":
                    is_workday = day % 7 not in [0, 6]  # 월~금
                elif config.work_pattern == "5days_weekend":
                    is_workday = day % 7 not in [0]  # 월~토 (일요일만 휴무)
                elif config.work_pattern == "3days":
                    is_workday = day % 7 in [1, 3, 5]  # 월, 수, 금
                elif config.work_pattern == "4days":
                    is_workday = day % 7 in [1, 2, 4, 5]  # 월, 화, 목, 금
                
                if is_workday and turn < 5:  # 오전 출근
                    is_overtime = random.random() < 0.3  # 30% 확률로 야근
                    if day % 7 in [0, 6]:  # 주말
                        earnings = self.simulate_weekend_work(stats)
                    else:
                        earnings = self.simulate_work_day(stats, is_overtime, config.difficulty)
                    stats.money += earnings
                    daily_earnings += earnings
                
                # 식사 (하루 2끼)
                if turn in [1, 7]:  # 아침, 저녁
                    food_type = self.choose_food(config.food_pattern, day, stats.money)
                    cost = self.eat_meal(stats, food_type, config.difficulty)
                    stats.money -= cost
                    daily_expenses += cost
                
                # VR 사용 (소유시 랜덤)
                if stats.has_vr and random.random() < 0.3 and turn > 5:
                    self.use_vr(stats)
                
                # 휴식 활동 (핸드폰, 게임 등)
                if turn > 5 and random.random() < 0.5:
                    stats.hp += 2
                    stats.san += 4
                
                # 포만감 자연 감소
                if turn % 3 == 0:  # 3턴마다
                    self.apply_satiety_decay(stats)
            
            # 하루 마무리
            self.process_sleep(stats)
            self.apply_yanggi_penalties(stats)
            
            # 스탯 제한
            stats.hp = max(0, min(100, stats.hp))
            stats.san = max(0, min(100, stats.san))
            stats.yanggi = max(0, min(100, stats.yanggi))
            stats.satiety = max(0, min(100, stats.satiety))
            
            # 일일 기록
            daily_money.append(stats.money)
            daily_hp.append(stats.hp)
            daily_san.append(stats.san)
            daily_yanggi.append(stats.yanggi)
            daily_satiety.append(stats.satiety)
            
            # 게임 오버 조건 체크
            if stats.hp <= 0:
                events.append(f"Day {day}: 게임 오버 (HP 0)")
                break
            if stats.san <= 0:
                events.append(f"Day {day}: 게임 오버 (SAN 0)")
                break
        
        return {
            "config": config,
            "days": list(range(1, len(daily_money) + 1)),
            "money": daily_money,
            "hp": daily_hp,
            "san": daily_san,
            "yanggi": daily_yanggi,
            "satiety": daily_satiety,
            "events": events,
            "stats": {
                "bankruptcy_day": next((i+1 for i, m in enumerate(daily_money) if m < 0), None),
                "hp_zero_day": next((i+1 for i, h in enumerate(daily_hp) if h <= 0), None),
                "san_zero_day": next((i+1 for i, s in enumerate(daily_san) if s <= 0), None),
                "yanggi_zero_day": next((i+1 for i, y in enumerate(daily_yanggi) if y <= 0), None),
                "vr_purchase_day": config.vr_purchase_day,
                "min_money": min(daily_money) if daily_money else 0,
                "max_money": max(daily_money) if daily_money else 0,
                "avg_money": sum(daily_money) / len(daily_money) if daily_money else 0
            }
        }
    
    def create_plots(self, results: Dict, output_dir: str) -> None:
        """그래프 생성"""
        config = results["config"]
        days = results["days"]
        
        # 그래프 크기 설정
        fig, ((ax1, ax2), (ax3, ax4)) = plt.subplots(2, 2, figsize=(15, 12))
        fig.suptitle(f'{config.name} - {config.difficulty.upper()} 모드', fontsize=16, fontweight='bold')
        
        # 1. 소지금 곡선
        ax1.plot(days, results["money"], 'g-', linewidth=2, label='소지금')
        ax1.axhline(y=0, color='r', linestyle='--', alpha=0.7, label='파산선')
        ax1.set_title('소지금 변화')
        ax1.set_xlabel('일수')
        ax1.set_ylabel('원')
        ax1.grid(True, alpha=0.3)
        ax1.legend()
        ax1.ticklabel_format(style='plain', axis='y')
        
        # 2. HP/SAN/양기 곡선
        ax2.plot(days, results["hp"], 'r-', linewidth=2, label='HP')
        ax2.plot(days, results["san"], 'b-', linewidth=2, label='SAN')
        ax2.plot(days, results["yanggi"], 'orange', linewidth=2, label='양기')
        ax2.axhline(y=30, color='orange', linestyle='--', alpha=0.5, label='양기 적정 하한')
        ax2.axhline(y=70, color='orange', linestyle='--', alpha=0.5, label='양기 적정 상한')
        ax2.set_title('HP/SAN/양기 변화')
        ax2.set_xlabel('일수')
        ax2.set_ylabel('수치')
        ax2.set_ylim(0, 100)
        ax2.grid(True, alpha=0.3)
        ax2.legend()
        
        # 3. 포만감 곡선
        ax3.plot(days, results["satiety"], 'm-', linewidth=2, label='포만감')
        ax3.axhline(y=20, color='r', linestyle='--', alpha=0.7, label='위험 구간')
        ax3.axhline(y=40, color='orange', linestyle='--', alpha=0.7, label='주의 구간')
        ax3.axhline(y=90, color='purple', linestyle='--', alpha=0.7, label='과식 구간')
        ax3.set_title('포만감 변화')
        ax3.set_xlabel('일수')
        ax3.set_ylabel('포만감')
        ax3.set_ylim(0, 100)
        ax3.grid(True, alpha=0.3)
        ax3.legend()
        
        # 4. 통계 정보 표시
        ax4.axis('off')
        stats = results["stats"]
        info_text = f"""
시나리오: {config.name}
난이도: {config.difficulty.upper()}
출근 패턴: {config.work_pattern}
식사 패턴: {config.food_pattern}
VR 구매일: {config.vr_purchase_day or '구매 안함'}

=== 주요 지표 ===
파산 시점: {stats["bankruptcy_day"] or '없음'}일
HP 0 도달: {stats["hp_zero_day"] or '없음'}일
SAN 0 도달: {stats["san_zero_day"] or '없음'}일
양기 0 도달: {stats["yanggi_zero_day"] or '없음'}일

=== 소지금 통계 ===
최소: {stats["min_money"]:,.0f}원
최대: {stats["max_money"]:,.0f}원
평균: {stats["avg_money"]:,.0f}원
        """
        ax4.text(0.1, 0.9, info_text, transform=ax4.transAxes, fontsize=11, 
                verticalalignment='top', fontfamily='monospace')
        
        plt.tight_layout()
        filename = f"{config.name.replace(' ', '_')}_{config.difficulty}.png"
        filepath = os.path.join(output_dir, filename)
        plt.savefig(filepath, dpi=150, bbox_inches='tight')
        plt.close()
        
        print(f"그래프 저장: {filepath}")
    
    def run_all_scenarios(self, output_dir: str) -> None:
        """모든 시나리오 실행"""
        scenarios = [
            SimulationConfig("1_최저생존", "5days", "frozen_only", None, "normal"),
            SimulationConfig("2_기본플레이", "5days", "frozen_delivery", 30, "normal"),
            SimulationConfig("3_열심히", "5days_weekend", "mixed", 20, "normal"),
            SimulationConfig("4_게으른플레이", "3days", "frozen_only", None, "normal"),
            SimulationConfig("5_이지모드", "4days", "frozen_only", 30, "easy"),
            SimulationConfig("6_하드모드", "5days", "frozen_only", 30, "hard")
        ]
        
        all_results = {}
        summary_data = []
        
        for config in scenarios:
            print(f"\n시뮬레이션 실행 중: {config.name}")
            results = self.simulate_scenario(config)
            all_results[config.name] = results
            
            # 그래프 생성
            self.create_plots(results, output_dir)
            
            # 요약 데이터 수집
            stats = results["stats"]
            summary_data.append({
                "시나리오": config.name,
                "난이도": config.difficulty.upper(),
                "파산일": stats["bankruptcy_day"] or "-",
                "HP_0일": stats["hp_zero_day"] or "-",
                "SAN_0일": stats["san_zero_day"] or "-",
                "양기_0일": stats["yanggi_zero_day"] or "-",
                "VR구매일": config.vr_purchase_day or "-",
                "최소소지금": f"{stats['min_money']:,.0f}",
                "최대소지금": f"{stats['max_money']:,.0f}",
                "평균소지금": f"{stats['avg_money']:,.0f}"
            })
        
        # 요약 결과 저장
        summary_file = os.path.join(output_dir, "simulation_summary.txt")
        with open(summary_file, 'w', encoding='utf-8') as f:
            f.write("NAICR 경제 밸런스 시뮬레이션 결과 요약\n")
            f.write("=" * 50 + "\n\n")
            
            # 테이블 형태로 출력
            headers = list(summary_data[0].keys())
            
            # 헤더 출력
            f.write(" | ".join(f"{h:^12}" for h in headers) + "\n")
            f.write("-" * (13 * len(headers) + len(headers) - 1) + "\n")
            
            # 데이터 출력
            for row in summary_data:
                f.write(" | ".join(f"{str(row[h]):^12}" for h in headers) + "\n")
            
            f.write("\n\n")
            f.write("상세 분석:\n")
            f.write("-" * 20 + "\n")
            
            for config_name, results in all_results.items():
                f.write(f"\n[{config_name}]\n")
                events = results["events"]
                if events:
                    f.write("주요 이벤트:\n")
                    for event in events[:10]:  # 최대 10개만
                        f.write(f"  - {event}\n")
                else:
                    f.write("특별한 이벤트 없음\n")
        
        print(f"\n요약 결과 저장: {summary_file}")
        print(f"모든 그래프가 {output_dir} 디렉토리에 저장되었습니다.")

def main():
    """메인 실행 함수"""
    output_dir = "/mnt/data/neverAloneInCheapRental/tools/sim_results"
    
    # 출력 디렉토리 생성
    os.makedirs(output_dir, exist_ok=True)
    
    # 시뮬레이터 실행
    simulator = NAICRSimulator()
    simulator.run_all_scenarios(output_dir)
    
    print("\n시뮬레이션 완료!")
    print(f"결과 확인: {output_dir}")

if __name__ == "__main__":
    main()