import React, { useState, useEffect } from 'react';
import { ApiService, Plan, User } from '../services/api';
import { X, Check, Zap, Crown, ShieldAlert } from 'lucide-react';

interface PricingModalProps {
  isOpen: boolean;
  onClose: () => void;
  currentUser: User;
  onUpgradeSuccess: (user: User) => void;
}

export const PricingModal: React.FC<PricingModalProps> = ({
  isOpen,
  onClose,
  currentUser,
  onUpgradeSuccess
}) => {
  const [plans, setPlans] = useState<Plan[]>([]);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (isOpen) {
      ApiService.getPlans().then((res) => setPlans(res.plans));
    }
  }, [isOpen]);

  if (!isOpen) return null;

  const handleUpgrade = async (planId: string) => {
    setLoading(true);
    try {
      const res = await ApiService.upgradePlan(planId);
      onUpgradeSuccess(res.user);
      onClose();
    } catch (err) {
      console.error('Upgrade failed', err);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/75 backdrop-blur-sm p-4">
      <div className="glass-card w-full max-w-4xl rounded-2xl p-6 shadow-2xl border border-slate-700 relative overflow-hidden max-h-[90vh] overflow-y-auto">
        <div className="flex items-center justify-between pb-4 border-b border-slate-800">
          <div className="flex items-center space-x-3">
            <div className="p-2.5 bg-blue-500/10 rounded-xl text-blue-400 border border-blue-500/20">
              <Crown className="w-6 h-6 text-amber-400" />
            </div>
            <div>
              <h2 className="text-lg font-bold text-slate-100">Upgrade Your AetherDesk SaaS License</h2>
              <p className="text-xs text-slate-400">Unlock Address Book, 64KB File Transfer & Unattended Access</p>
            </div>
          </div>
          <button onClick={onClose} className="p-2 text-slate-400 hover:text-white rounded-xl">
            <X className="w-5 h-5" />
          </button>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-3 gap-6 my-6">
          {plans.map((plan) => {
            const isCurrent = (currentUser.plan || 'FREE') === plan.id;
            return (
              <div
                key={plan.id}
                className={`glass-card rounded-2xl p-6 border flex flex-col justify-between relative transition-all ${
                  plan.popular
                    ? 'border-blue-500 shadow-xl shadow-blue-500/10 bg-blue-950/20'
                    : 'border-slate-800'
                }`}
              >
                {plan.popular && (
                  <span className="absolute top-3 right-3 px-2.5 py-0.5 bg-blue-500 text-white font-semibold text-[10px] uppercase rounded-full tracking-wider">
                    MOST POPULAR
                  </span>
                )}

                <div>
                  <h3 className="text-base font-bold text-slate-100 mb-1">{plan.name}</h3>
                  <div className="flex items-baseline space-x-1 mb-4">
                    <span className="text-3xl font-bold text-white">{plan.price}</span>
                    <span className="text-xs text-slate-400">/{plan.period}</span>
                  </div>

                  <ul className="space-y-2.5 text-xs text-slate-300 mb-6">
                    {plan.features.map((feat, idx) => (
                      <li key={idx} className="flex items-center space-x-2">
                        <Check className="w-4 h-4 text-emerald-400 flex-shrink-0" />
                        <span>{feat}</span>
                      </li>
                    ))}
                  </ul>
                </div>

                <button
                  onClick={() => handleUpgrade(plan.id)}
                  disabled={loading || isCurrent}
                  className={`w-full py-2.5 rounded-xl font-medium text-xs transition-all ${
                    isCurrent
                      ? 'bg-slate-800 text-slate-400 cursor-default'
                      : plan.popular
                      ? 'bg-gradient-to-r from-blue-600 to-indigo-600 hover:from-blue-500 text-white shadow-lg shadow-blue-500/30'
                      : 'bg-slate-800 hover:bg-slate-700 text-slate-100'
                  }`}
                >
                  {isCurrent ? 'Current Plan' : `Upgrade to ${plan.name}`}
                </button>
              </div>
            );
          })}
        </div>
      </div>
    </div>
  );
};
