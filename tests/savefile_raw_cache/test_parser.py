"""Synthetic parser adversaries; not native evidence."""
import importlib.util
from pathlib import Path
import tempfile
import unittest
import xml.etree.ElementTree as ET

HERE=Path(__file__).resolve().parent
spec=importlib.util.spec_from_file_location('raw_verify',HERE/'verify.py');verify=importlib.util.module_from_spec(spec);spec.loader.exec_module(verify)
URI=verify.NS['t']
def node(parent,tag,**attrs):return ET.SubElement(parent,'{'+URI+'}'+tag,attrs)

def fixture(mode='positive'):
    root=ET.Element('{'+URI+'}TestRun');results=node(root,'Results');row=node(results,'UnitTestResult',testName=verify.METHOD,testId='a',outcome='Failed' if mode=='drop-inmemory' else 'Passed')
    output=node(row,'Output');stdout=node(output,'StdOut');retain=mode in ('baseline','omit-release')
    values={'valid-open':128 if retain else 0,'valid-close':0,'malformed-unique':1048576 if retain else 0,'malformed-repeat':65536 if retain else 0,'deleted-close':128 if retain else 0,'serialize-failure':128 if retain else 0,'append-failure':128 if retain else 0}
    stdout.text='\n'.join(f'RAW_CACHE_ROW {name} bytes={n} expected={n} paths={verify.ROWS[name]}' for name,n in values.items())+'\n'
    if mode=='drop-inmemory':
        error=node(output,'ErrorInfo');node(error,'Message').text='RAW_CACHE_EMBEDDED_UNCHANGED\nAssert.That(embedded.ResourceData, Is.SameAs(embeddedBytes))\n  Expected: same as < 1, 2, 3 >\n  But was:  null';node(error,'StackTrace').text='   at '+verify.CLASS+'.Check(...) in Content.IntegrationTests/SavefileRawCacheTests.cs:line 200'
    else:stdout.text+=f'RAW_CACHE_COMPLETE mode={mode} unique=16 size=65536 buckets=clean identity=preserved\n'
    definitions=node(root,'TestDefinitions');definition=node(definitions,'UnitTest',id='a');node(definition,'TestMethod',className=verify.CLASS,name=verify.METHOD)
    summary=node(root,'ResultSummary');node(summary,'Counters',total='1',executed='1',passed='0' if mode=='drop-inmemory' else '1',failed='1' if mode=='drop-inmemory' else '0')
    return root

class ParserTests(unittest.TestCase):
    def run_fixture(self,mode='positive',change=None,code=None,log=None):
        root=fixture(mode)
        if log is None:log=('Failed' if mode=='drop-inmemory' else 'Passed')+' '+verify.METHOD+'\nTest Run '+('Failed.' if mode=='drop-inmemory' else 'Successful.')+'\n'
        if change:change(root)
        with tempfile.TemporaryDirectory() as tmp:
            path=Path(tmp)/'synthetic.trx';ET.ElementTree(root).write(path,encoding='utf-8')
            return verify.verify(path,mode,(1 if mode=='drop-inmemory' else 0) if code is None else code,log)
    def reject(self,change=None,code=None,log=None):
        with self.assertRaises(AssertionError):self.run_fixture(change=change,code=code,log=log)
    def test_four_declared_modes(self):
        for mode in ('positive','baseline','omit-release','drop-inmemory'):self.assertTrue(self.run_fixture(mode))
    def test_unknown_counter(self):self.reject(lambda r:r.find('.//t:Counters',verify.NS).set('unexpected','0'))
    def test_unknown_row_spelling(self):self.reject(lambda r:setattr(r.find('.//t:StdOut',verify.NS),'text',r.find('.//t:StdOut',verify.NS).text+'RAW_CACHE_ROW corrupted bytes=bad\n'))
    def test_signal_exit_is_not_expected_failure(self):
        with self.assertRaises(AssertionError):self.run_fixture('drop-inmemory',code=139)
    def test_missing_native_runner_footer(self):self.reject(log='Passed '+verify.METHOD)
    def test_negative_known_engine_and_async_frames(self):
        def change(r):
            stack=r.find('.//t:StackTrace',verify.NS)
            stack.text+='\n   at System.Threading.Thread.StartHelper.Callback(Object state)\n--- End of stack trace from previous location ---\n   at NUnit.Framework.Internal.TaskAwaitAdapter.GenericAdapter`1.BlockUntilCompleted()'
        self.assertTrue(self.run_fixture('drop-inmemory',change=change))
    def test_missing_row(self):self.reject(lambda r:setattr(r.find('.//t:StdOut',verify.NS),'text',r.find('.//t:StdOut',verify.NS).text.replace('RAW_CACHE_ROW valid-close bytes=0 expected=0 paths=1\n','')))
    def test_duplicate_row(self):self.reject(lambda r:setattr(r.find('.//t:StdOut',verify.NS),'text',r.find('.//t:StdOut',verify.NS).text+'RAW_CACHE_ROW valid-close bytes=0 expected=0 paths=1\n'))
    def test_wrong_byte_bound(self):self.reject(lambda r:setattr(r.find('.//t:StdOut',verify.NS),'text',r.find('.//t:StdOut',verify.NS).text.replace('malformed-unique bytes=0 expected=0','malformed-unique bytes=1 expected=1')))
    def test_wrong_path_count(self):self.reject(lambda r:setattr(r.find('.//t:StdOut',verify.NS),'text',r.find('.//t:StdOut',verify.NS).text.replace('paths=16','paths=17')))
    def test_wrong_process_exit(self):self.reject(code=1)
    def test_runtime_error(self):self.reject(log='[ERRO] actual runtime failure')
    def test_wrong_definition(self):self.reject(lambda r:r.find('.//t:TestMethod',verify.NS).set('className','Other.Fixture'))
    def test_skipped_counter(self):self.reject(lambda r:r.find('.//t:Counters',verify.NS).set('notExecuted','1'))
    def test_runinfo(self):self.reject(lambda r:node(r,'RunInfo'))
    def test_duplicate_test(self):self.reject(lambda r:node(r.find('t:Results',verify.NS),'UnitTestResult',testName=verify.METHOD,testId='b',outcome='Passed'))
    def test_unintended_negative_message(self):
        with self.assertRaises(AssertionError):self.run_fixture('drop-inmemory',lambda r:setattr(r.find('.//t:Message',verify.NS),'text','unrelated assertion'))
    def test_foreign_negative_stack(self):
        with self.assertRaises(AssertionError):self.run_fixture('drop-inmemory',lambda r:setattr(r.find('.//t:StackTrace',verify.NS),'text',r.find('.//t:StackTrace',verify.NS).text+'\n at Other.Engine.Fault()'))

if __name__=='__main__':unittest.main()
