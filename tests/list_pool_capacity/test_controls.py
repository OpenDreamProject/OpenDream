import json
from pathlib import Path
import tempfile
import unittest
import xml.etree.ElementTree as ET
from control import LIST, ROOT, mutate
from verify import CLASS, FAILURES, NS, TESTS, verify, load_outputs

def fixture(mode):
    tag=lambda value:'{'+NS['t']+'}'+value
    root=ET.Element(tag('TestRun'));results=ET.SubElement(root,tag('Results'));definitions=ET.SubElement(root,tag('TestDefinitions'))
    def text(parent,name,value):ET.SubElement(parent,tag(name)).text=value
    baseline=mode=='baseline'
    retained={'count':32 if baseline else 0,'slots':32*131072 if baseline else 0,'maximum':131072 if baseline else 0,
              'alive':32 if baseline else 0,'retired':32,'valueBytes':24,'backingPayloadBytes':32*131072*24 if baseline else 0}
    child={'count':0,'childAlive':False,'entries':[]} if mode=='no-pooling' else {'count':1,'childAlive':mode=='nonclearing','entries':[2 if mode=='nonclearing' else 0]}
    metrics=[]
    for size in [32,4096,65536,65537,131072,350000]:
        pooled=baseline and size>2048 or mode!='no-pooling' and 2048<size<=65536
        metrics.append({'size':size,'count':8,'allocated':100,'seconds':0.1,'collections':[0,0,0],'poolCount':int(pooled),'slots':size if pooled else 0,'valueBytes':24})
    mixed={'beforeMedium':350000,'afterMedium':350000,'poolCount':1} if baseline else {'beforeMedium':0,'afterMedium':0 if mode=='no-pooling' else 4096,'poolCount':0 if mode=='no-pooling' else 1}
    stdout={'RetiredCapacityAndWeakRelease':'POOL_RETENTION '+json.dumps(retained),
            'SemanticsReferencesAndMediumReuse':'POOL_CHILD '+json.dumps(child),
            'ComparableWorkloads':'\n'.join('POOL_METRIC '+json.dumps(row) for row in metrics)+'\nPOOL_MIXED '+json.dumps(mixed)}
    for name in sorted(TESTS):
        failed=name in FAILURES[mode]
        row=ET.SubElement(results,tag('UnitTestResult'),{'testName':name,'testId':name,'outcome':'Failed' if failed else 'Passed'})
        output=ET.SubElement(row,tag('Output'));text(output,'StdOut',stdout[name])
        if failed:
            error=ET.SubElement(output,tag('ErrorInfo'))
            text(error,'Message',FAILURES[mode][name]+' Assert.That(value, constraint) Expected: expected But was: actual')
            text(error,'StackTrace',CLASS+'.'+name+'()')
        unit=ET.SubElement(definitions,tag('UnitTest'),{'id':name});ET.SubElement(unit,tag('TestMethod'),{'className':CLASS,'name':name})
    failed_count=len(FAILURES[mode])
    ET.SubElement(ET.SubElement(root,tag('ResultSummary')),tag('Counters'),total='3',executed='3',passed=str(3-failed_count),failed=str(failed_count))
    return ET.tostring(root,encoding='unicode'),('Test Run Failed.' if FAILURES[mode] else 'Test Run Successful.'),int(bool(FAILURES[mode]))

class Controls(unittest.TestCase):
    def test_manifest_and_original_archive_are_authenticated(self):
        import hashlib
        pins=json.loads((ROOT/'tests/list_pool_capacity/source-manifest.json').read_text(encoding='utf-8'))['sources']
        for name,pin in pins.items():
            self.assertEqual(hashlib.sha256((ROOT/name).read_bytes().replace(b'\r\n',b'\n')).hexdigest(),pin,name)
        workflow=(ROOT/'.github/workflows/list-pool-capacity.yml').read_text(encoding='utf-8')
        self.assertIn('archive_logs.py pool-results --verify',workflow)
        self.assertIn('include-hidden-files: true',workflow)
        self.assertIn('GameTests.NoRuntimesTest',workflow)
    def test_positive_identical(self):
        self.assertEqual(mutate(b'arbitrary','positive'),b'arbitrary')
    def test_nonclearing_exact_and_guards(self):
        needle=b'            _values.Clear();'
        changed=mutate(b'before\n'+needle+b'\nafter','nonclearing')
        self.assertNotIn(b'_values.Clear();',changed)
        for raw in (b'missing',needle*2):
            with self.assertRaises(AssertionError):mutate(raw,'nonclearing')
    def test_no_pooling_exact_and_guards(self):
        needle=b'ListPool.Count < DreamManager.ListPoolSize'
        self.assertIn(b'ListPool.Count < 0',mutate(needle,'no-pooling'))
        for raw in (b'missing',needle*2):
            with self.assertRaises(AssertionError):mutate(raw,'no-pooling')
    def test_production_bound_is_return_only(self):
        raw=(ROOT/LIST).read_bytes()
        self.assertEqual(raw.count(b'private const int MaximumPooledCapacity = 65_536;'),1)
        self.assertEqual(raw.count(b'_values.Capacity <= MaximumPooledCapacity'),1)
        self.assertIn(b'if (size >= DreamManager.ListPoolThreshold && ListPool.TryPop',raw)
    def test_all_classifications(self):
        with tempfile.TemporaryDirectory() as temp:
            for mode in FAILURES:verify(*fixture(mode)[:2],mode,fixture(mode)[2],Path(temp))
    def test_fault_rejection(self):
        with tempfile.TemporaryDirectory() as temp:
            for mode in FAILURES:
                trx,log,code=fixture(mode)
                for fault in ['[ERRO] fixture fault','Unhandled exception','error CS0123','runtime error']:
                    with self.assertRaises(AssertionError):verify(trx,log+'\n'+fault,mode,code,Path(temp))
                with self.assertRaises(AssertionError):verify(trx,log,mode,2,Path(temp))
    def test_identity_counters_and_foreign_diagnostics(self):
        trx, log, code = fixture('positive')
        tag=lambda value:'{'+NS['t']+'}'+value
        with tempfile.TemporaryDirectory() as temp:
            for fault in ['duplicate-id','mismatched-id','wrong-method','error-counter','total-counter','run-info','stderr','foreign-stdout','extra-error']:
                root=ET.fromstring(trx)
                rows=root.findall('.//t:UnitTestResult',NS)
                if fault=='duplicate-id': rows[1].set('testId',rows[0].get('testId'))
                elif fault=='mismatched-id': rows[0].set('testId','foreign')
                elif fault=='wrong-method':root.find('.//t:TestMethod',NS).set('name','foreign')
                elif fault.endswith('counter'): root.find('.//t:Counters',NS).set('error' if fault=='error-counter' else 'total','1')
                elif fault=='run-info':ET.SubElement(root,tag('RunInfo')).text='foreign'
                elif fault=='stderr':ET.SubElement(root,tag('StdErr')).text='foreign'
                elif fault=='foreign-stdout':root.find('.//t:StdOut',NS).text+='\n[ERRO] foreign'
                else:ET.SubElement(root,tag('ErrorInfo'))
                with self.assertRaises(AssertionError): verify(ET.tostring(root,encoding='unicode'),log,'positive',code,Path(temp))
    def test_single_process_loader_requires_exact_identity(self):
        tag=lambda value:'{'+NS['t']+'}'+value
        with tempfile.TemporaryDirectory() as temp:
            directory=Path(temp)
            for mode in FAILURES:
                aggregate,_,_=fixture(mode);root=ET.fromstring(aggregate)
                for name in TESTS:
                    single=ET.Element(tag('TestRun'))
                    results=ET.SubElement(single,tag('Results'));results.append(next(row for row in root.findall('.//t:UnitTestResult',NS) if row.get('testName')==name))
                    definitions=ET.SubElement(single,tag('TestDefinitions'));definitions.append(next(row for row in root.findall('.//t:UnitTest',NS) if row.get('id')==name))
                    failed=int(name in FAILURES[mode])
                    ET.SubElement(ET.SubElement(single,tag('ResultSummary')),tag('Counters'),total='1',executed='1',passed=str(1-failed),failed=str(failed))
                    (directory/('pool-'+name+'.trx')).write_text(ET.tostring(single,encoding='unicode'))
                    (directory/('pool-'+name+'.log')).write_text('Test Run Failed.' if failed else 'Test Run Successful.')
                    (directory/('pool-'+name+'-exit-code.txt')).write_text(str(failed))
                trx,log,code=load_outputs(directory,mode);verify(trx,log,mode,code,directory/'derived')
                path=directory/'pool-ComparableWorkloads.trx';raw=path.read_text();path.write_text(raw.replace('testId="ComparableWorkloads"','testId="foreign"'))
                with self.assertRaises(AssertionError):load_outputs(directory,mode)

    def test_metrics_and_class_drift_rejected(self):
        trx,log,code=fixture('positive')
        with tempfile.TemporaryDirectory() as temp:
            for drift in [trx.replace(CLASS,CLASS+'Other'),trx.replace('"retired": 32','"retired": 31'),trx.replace('"slots": 65536','"slots": 99999')]:
                with self.assertRaises(AssertionError):verify(drift,log,'positive',code,Path(temp))

if __name__=='__main__':unittest.main()
