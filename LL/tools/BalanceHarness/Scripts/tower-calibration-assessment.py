"""Versioned global acceptance for explicitly larger, verified campaigns."""
import math
import statistics

VERSION = 'global-bonferroni-wilson-95-v2'
MAXIMUM_FAMILY = 3000
MAXIMUM_COMBATS = 750000


def assess(frozen_ids, verified, protocol):
    n = protocol['confirmationSamples']
    if (protocol.get('campaignAssessmentPolicy') != VERSION or n != 250
            or not frozen_ids or len(frozen_ids) != len(set(frozen_ids))
            or len(frozen_ids) != protocol['familySize'] or len(frozen_ids) > MAXIMUM_FAMILY
            or not len(frozen_ids)*n <= protocol['maximumActualBattles'] <= MAXIMUM_COMBATS):
        raise ValueError('Invalid explicitly bounded global campaign.')
    rows = [c for p in verified for c in p['cells']]
    if len(rows) != len(frozen_ids) or {r['id'] for r in rows} != set(frozen_ids):
        raise ValueError('Missing, duplicate or additional global evidence.')
    if any(p['issues'] or p['assessment'] == 'Invalid' for p in verified) or any(
            r['issues'] or r['valid'] != n or not 0 <= r['wins'] <= n for r in rows):
        raise ValueError('Unverified or incomplete partition evidence.')
    z = statistics.NormalDist().inv_cdf(1-.025/len(rows)); checks = []
    for r in rows:
        rate = r['wins']/n; divisor = 1+z*z/n
        center = (rate+z*z/(2*n))/divisor
        width = z*math.sqrt(rate*(1-rate)/n+z*z/(4*n*n))/divisor
        checks.append({'id': r['id'], 'wins': r['wins'], 'valid': n, 'draws': r['draws'], 'rate': rate,
            'lower': max(0,center-width), 'upper': min(1,center+width), 'artifactHash': r['artifactHash']})
    above = any(r['rate'] > .5 for r in checks)
    upper = all(r['upper'] <= .5 for r in checks); viable = any(r['lower'] >= .1 for r in checks)
    status = 'Fail' if above or all(r['upper'] < .1 for r in checks) else 'Pass' if upper and viable else 'Inconclusive'
    return {'assessment': status, 'assessmentPolicy': VERSION, 'familySize': len(rows), 'familyConfidence': .95,
        'z': z, 'upperSupported': upper, 'viabilitySupported': viable, 'observedAboveCeiling': above, 'cells': checks,
        'scope': 'Complete frozen portfolio, one approximate Bonferroni-Wilson family. Partition acceptance labels are ignored. Unsearched teams remain unvalidated.'}
