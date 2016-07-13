using System;
namespace Sean.WorldGenerator.Noise
{
    enum EUnaryMathOperation
    {
        ACOS,
        ASIN,
        ATAN,
        COS,
        SIN,
        TAN,
        ABS,
        FLOOR,
        CEIL,
        EXP,
        LOG10,
        LOG2,
        LOGN,
        ONEMINUS,
        SQRT,
        INTEGER,
        FRACTIONAL,
		EASECUBIC,
		EASEQUINTIC,
    };

    enum EBinaryMathOperation
    {
        POW=EASEQUINTIC+1,
        FMOD,
        BIAS,
        GAIN,
        PMINUS,
        SUM,
        MULTIPLY,
        DIVIDE,
        SUBTRACT,
        MAXIMUM,
        MINIMUM
    };

    class CImplicitMath : CImplicitModuleBase
    {
        public CImplicitMath();
        public CImplicitMath(unsigned int op, float source=1, float p=1);
        public CImplicitMath(unsigned int op, CImplicitModuleBase * source=0, float p=1);
        public CImplicitMath(unsigned int op, float source=0, CImplicitModuleBase * p=0);
        public CImplicitMath(unsigned int op, CImplicitModuleBase * source=0, CImplicitModuleBase * p=0);

        public void setSource(float v);
        public void setSource(CImplicitModuleBase * b);
        public void setParameter(float v);
        public void setParameter(CImplicitModuleBase * b);
        public void setOperation(unsigned int op);

        public float get(float x, float y);
        public float get(float x, float y, float z);
        public float get(float x, float y, float z, float w);
        public float get(float x, float y, float z, float w, float u, float v);

        protected:
        unsigned int m_op;
        CScalarParameter m_source;
        CScalarParameter m_parameter;

        float applyOp(float v, float p);



    };
};

#endif
#include <cmath>
#include <algorithm>
#include "implicitmath.h"
#include "utility.h"

namespace anl
{
    CImplicitMath::CImplicitMath() : CImplicitModuleBase(), m_op(ABS), m_source(0.0), m_parameter(0.0){}

    CImplicitMath::CImplicitMath(unsigned int op, float source, float p) : CImplicitModuleBase(), m_op(op), m_source(source), m_parameter(p){}
    CImplicitMath::CImplicitMath(unsigned int op, CImplicitModuleBase * source, float p) : CImplicitModuleBase(), m_op(op), m_source(source), m_parameter(p){}
    CImplicitMath::CImplicitMath(unsigned int op, float source, CImplicitModuleBase * p) : CImplicitModuleBase(), m_op(op), m_source(source), m_parameter(p){}
    CImplicitMath::CImplicitMath(unsigned int op, CImplicitModuleBase * source, CImplicitModuleBase * p) : CImplicitModuleBase(), m_op(op), m_source(source), m_parameter(p){}
    CImplicitMath::~CImplicitMath() {}

    void CImplicitMath::setSource(float v)
    {
        m_source.set(v);
    }

    void CImplicitMath::setSource(CImplicitModuleBase * b)
    {
        m_source.set(b);
    }

    void CImplicitMath::setParameter(float v)
    {
        m_parameter.set(v);
    }

    void CImplicitMath::setParameter(CImplicitModuleBase * b)
    {
        m_parameter.set(b);
    }

    void CImplicitMath::setOperation(unsigned int op)
    {
        m_op=op;
    }

    float CImplicitMath::get(float x, float y)
    {
        float v=m_source.get(x,y);
        float p=m_parameter.get(x,y);
        return applyOp(v,p);
    }

    float CImplicitMath::get(float x, float y, float z)
    {
        float v=m_source.get(x,y,z);
        float p=m_parameter.get(x,y,z);
        return applyOp(v,p);
    }

    float CImplicitMath::get(float x, float y, float z, float w)
    {
        float v=m_source.get(x,y,z,w);
        float p=m_parameter.get(x,y,z,w);
        return applyOp(v,p);
    }

    float CImplicitMath::get(float x, float y, float z, float w, float u, float v)
    {
        float val=m_source.get(x,y,z,w,u,v);
        float p=m_parameter.get(x,y,z,w,u,v);
        return applyOp(val,p);
    }


    float CImplicitMath::applyOp(float v, float p)
    {
        switch(m_op)
        {
            case ACOS: return acos(v); break;
            case ASIN: return asin(v); break;
            case ATAN: return atan(v); break;
            case COS: return cos(v); break;
            case SIN: return sin(v); break;
            case TAN: return tan(v); break;
            case ABS: return (v<0) ? -v : v; break;
            case FLOOR: return floor(v); break;
            case CEIL: return ceil(v); break;
            case POW: return pow(v,p); break;
            case EXP: return exp(v); break;
            case LOG10: return log10(v); break;
            case LOG2: return log(v)/log(2.0); break;
            case LOGN: return log(v); break;
            case FMOD: return fmod(v,p); break;
            case BIAS: return bias(p,v); break;
            case GAIN: return gain(p,v); break;
            case ONEMINUS: return 1.0-v; break;
            case PMINUS: return p-v; break;
            case SQRT: return sqrt(v); break;
            case INTEGER: return (float)(int)v; break;
            case FRACTIONAL: return v-(float)(int)v; break;
			case EASECUBIC: return hermite_blend(v); break;
			case EASEQUINTIC: return quintic_blend(v); break;
            case SUM: return v+p; break;
            case MULTIPLY: return v*p; break;
            case DIVIDE: return v/p; break;
            case SUBTRACT: return v-p; break;
            case MAXIMUM: return std::max(v,p); break;
            case MINIMUM: return std::min(v,p); break;
            default: return v; break;
        }
    }
}
